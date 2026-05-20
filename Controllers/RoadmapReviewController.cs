using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize]
public sealed class RoadmapReviewController(
    AppDbContext dbContext,
    IFileStorageService storageService) : ControllerBase
{
    private const long MaxEvidenceFileSize = 25 * 1024 * 1024; // 25 MB
    private static readonly string[] AllowedEvidenceContentTypes =
    [
        "application/zip",
        "application/x-zip-compressed",
        "application/octet-stream",
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/jpg"
    ];
    private static readonly string[] AllowedEvidenceExtensions =
    [
        ".zip", ".pdf", ".png", ".jpg", ".jpeg"
    ];

    private const int FreePlanReviewLimit = 2;

    // ============================================================
    //  STUDENT ENDPOINTS
    // ============================================================

    [HttpGet("api/roadmap-node/{id:guid}/available-reviewers")]
    public async Task<ActionResult<AvailableReviewersResponse>> GetAvailableReviewers(
        Guid id,
        CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();

        var node = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Include(item => item.Roadmap)
            .SingleOrDefaultAsync(item => item.Id == id && item.Roadmap.UserId == studentId, cancellationToken);

        if (node is null)
        {
            return NotFound(new { message = "Roadmap node not found." });
        }

        // Counselor đã được admin gán cho student này
        var counselor = await dbContext.CounselorAssignments
            .AsNoTracking()
            .Where(item => item.StudentId == studentId && item.Status == "Active")
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => item.Counselor)
            .FirstOrDefaultAsync(cancellationToken);

        AvailableReviewerInfo? counselorInfo = null;
        if (counselor is not null)
        {
            var counselorHasPending = await dbContext.RoadmapNodeReviewRequests
                .AsNoTracking()
                .AnyAsync(item => item.RoadmapNodeId == id
                    && item.ReviewerId == counselor.Id
                    && item.Status == "Pending", cancellationToken);

            counselorInfo = new AvailableReviewerInfo(
                counselor.Id,
                counselor.FullName,
                counselor.Email,
                counselor.AvatarUrl,
                UserRoles.AcademicCounselor,
                Quota: null,
                Used: null,
                Remaining: null,
                Available: !counselorHasPending);
        }

        // Industry Mentors: tất cả mentor active, có quota > 0 cho student này
        var mentors = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRoles.IndustryMentor && user.IsActive)
            .ToListAsync(cancellationToken);

        var mentorInfos = new List<AvailableReviewerInfo>();
        foreach (var mentor in mentors)
        {
            var quota = await CalculateMentorQuotaAsync(studentId, cancellationToken);

            // Q2 = A: hide mentor hết quota
            if (quota.Remaining <= 0)
            {
                continue;
            }

            var hasPendingForNode = await dbContext.RoadmapNodeReviewRequests
                .AsNoTracking()
                .AnyAsync(item => item.RoadmapNodeId == id
                    && item.ReviewerId == mentor.Id
                    && item.Status == "Pending", cancellationToken);

            mentorInfos.Add(new AvailableReviewerInfo(
                mentor.Id,
                mentor.FullName,
                mentor.Email,
                mentor.AvatarUrl,
                UserRoles.IndustryMentor,
                Quota: quota.Limit,
                Used: quota.Used,
                Remaining: quota.Remaining,
                Available: !hasPendingForNode));
        }

        return Ok(new AvailableReviewersResponse(counselorInfo, mentorInfos));
    }

    [HttpPost("api/roadmap-node/{id:guid}/review-requests")]
    public async Task<ActionResult<RoadmapReviewRequestResponse>> CreateReviewRequest(
        Guid id,
        CreateReviewRequestRequest request,
        CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();

        var node = await dbContext.RoadmapNodes
            .Include(item => item.Roadmap)
            .SingleOrDefaultAsync(item => item.Id == id && item.Roadmap.UserId == studentId, cancellationToken);

        if (node is null)
        {
            return NotFound(new { message = "Roadmap node not found." });
        }

        if (node.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Cannot request review for group nodes." });
        }

        if (node.Status is not ("Completed" or "NeedReview"))
        {
            return BadRequest(new { message = "Node must be Completed before requesting review." });
        }

        // Validate reviewer
        var reviewer = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == request.ReviewerId && user.IsActive, cancellationToken);

        if (reviewer is null)
        {
            return BadRequest(new { message = "Reviewer not found." });
        }

        if (reviewer.Role != UserRoles.AcademicCounselor && reviewer.Role != UserRoles.IndustryMentor)
        {
            return BadRequest(new { message = "Selected user cannot review roadmaps." });
        }

        // If counselor: must be assigned to this student
        if (reviewer.Role == UserRoles.AcademicCounselor)
        {
            var isAssigned = await dbContext.CounselorAssignments
                .AsNoTracking()
                .AnyAsync(item => item.StudentId == studentId
                    && item.CounselorId == reviewer.Id
                    && item.Status == "Active", cancellationToken);

            if (!isAssigned)
            {
                return BadRequest(new { message = "This counselor is not assigned to you." });
            }
        }

        // If mentor: check quota
        if (reviewer.Role == UserRoles.IndustryMentor)
        {
            var quota = await CalculateMentorQuotaAsync(studentId, cancellationToken);
            if (quota.Remaining <= 0)
            {
                return StatusCode(StatusCodes.Status402PaymentRequired, new
                {
                    message = $"Sinh viên đã hết lượt mentor review ({quota.Used}/{quota.Limit}). Hãy nâng cấp gói.",
                    quota
                });
            }
        }

        var now = DateTimeOffset.UtcNow;

        // Auto cancel any existing pending request for this node
        var existingPending = await dbContext.RoadmapNodeReviewRequests
            .Where(item => item.RoadmapNodeId == id
                && item.StudentId == studentId
                && item.Status == "Pending")
            .ToListAsync(cancellationToken);

        foreach (var pending in existingPending)
        {
            pending.Status = "Cancelled";
            pending.RespondedAt = now;
        }

        var reviewRequest = new RoadmapNodeReviewRequest
        {
            Id = Guid.NewGuid(),
            RoadmapNodeId = id,
            StudentId = studentId,
            ReviewerId = reviewer.Id,
            ReviewerRole = reviewer.Role,
            Status = "Pending",
            StudentNote = request.StudentNote?.Trim(),
            EvidenceUrl = request.EvidenceUrl?.Trim(),
            EvidenceType = request.EvidenceType?.Trim(),
            EvidenceFileName = request.EvidenceFileName?.Trim(),
            RequestedAt = now
        };

        dbContext.RoadmapNodeReviewRequests.Add(reviewRequest);

        // Auto move node status to NeedReview if currently Completed
        if (node.Status == "Completed")
        {
            node.Status = "NeedReview";
            node.UpdatedAt = now;
        }

        // Send notification to reviewer
        var student = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == studentId)
            .Select(user => new { user.FullName })
            .SingleOrDefaultAsync(cancellationToken);

        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = reviewer.Id,
            Type = "RoadmapReviewRequested",
            Title = "Có yêu cầu review mới",
            Message = $"{student?.FullName ?? "Sinh viên"} đã gửi yêu cầu review cho module \"{node.Title}\".",
            LinkUrl = "#roadmap-reviews",
            CreatedAt = now,
            IsRead = false
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await ToResponseAsync(reviewRequest, cancellationToken));
    }

    [HttpPost("api/roadmap-node/review-requests/{requestId:guid}/cancel")]
    public async Task<ActionResult<RoadmapReviewRequestResponse>> CancelReviewRequest(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();

        var reviewRequest = await dbContext.RoadmapNodeReviewRequests
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (reviewRequest is null)
        {
            return NotFound(new { message = "Review request not found." });
        }

        if (reviewRequest.StudentId != studentId)
        {
            return Forbid();
        }

        if (reviewRequest.Status != "Pending")
        {
            return BadRequest(new { message = "Only pending requests can be cancelled." });
        }

        reviewRequest.Status = "Cancelled";
        reviewRequest.RespondedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await ToResponseAsync(reviewRequest, cancellationToken));
    }

    [HttpGet("api/roadmap-node/{id:guid}/review-requests")]
    public async Task<ActionResult<IReadOnlyList<RoadmapReviewRequestResponse>>> GetReviewRequestsForNode(
        Guid id,
        CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();

        var node = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Include(item => item.Roadmap)
            .SingleOrDefaultAsync(item => item.Id == id && item.Roadmap.UserId == studentId, cancellationToken);

        if (node is null)
        {
            return NotFound(new { message = "Roadmap node not found." });
        }

        var list = await dbContext.RoadmapNodeReviewRequests
            .AsNoTracking()
            .Include(item => item.Reviewer)
            .Where(item => item.RoadmapNodeId == id)
            .OrderByDescending(item => item.RequestedAt)
            .ToListAsync(cancellationToken);

        var responses = list.Select(item => ToResponse(item, item.Reviewer)).ToList();
        return Ok(responses);
    }

    // ============================================================
    //  STORAGE — Upload evidence
    // ============================================================

    [HttpPost("api/storage/roadmap-evidence")]
    [RequestSizeLimit(MaxEvidenceFileSize)]
    public async Task<ActionResult<EvidenceUploadResponse>> UploadEvidence(
        [FromForm] UploadEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length <= 0)
        {
            return BadRequest(new { message = "File is required." });
        }

        if (request.File.Length > MaxEvidenceFileSize)
        {
            return BadRequest(new { message = "File exceeds 25 MB limit." });
        }

        var fileName = request.File.FileName;
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedEvidenceExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                message = $"File type {extension} not allowed. Use {string.Join(", ", AllowedEvidenceExtensions)}."
            });
        }

        var contentType = request.File.ContentType?.ToLowerInvariant() ?? string.Empty;
        if (!AllowedEvidenceContentTypes.Contains(contentType))
        {
            // Allow octet-stream for zip uploaded by some browsers
            if (contentType != "application/octet-stream")
            {
                return BadRequest(new { message = $"Content type {contentType} not allowed." });
            }
        }

        var userId = GetCurrentUserId();
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
        var safeName = Guid.NewGuid().ToString("N");
        var objectName = $"users/{userId}/roadmap-evidence/{timestamp}-{safeName}{extension}";

        await using var stream = request.File.OpenReadStream();
        var result = await storageService.UploadAsync(stream, objectName, request.File.ContentType, cancellationToken);

        var evidenceType = extension switch
        {
            ".zip" => "ZipArchive",
            ".pdf" => "Document",
            ".png" or ".jpg" or ".jpeg" => "Image",
            _ => "File"
        };

        return Ok(new EvidenceUploadResponse(
            result.ObjectName,
            fileName,
            request.File.Length,
            request.File.ContentType ?? "application/octet-stream",
            evidenceType));
    }

    // ============================================================
    //  REVIEWER ENDPOINTS
    // ============================================================

    [Authorize(Roles = UserRoles.IndustryMentor)]
    [HttpGet("api/industry-mentor/roadmap-review-queue")]
    public async Task<ActionResult<IReadOnlyList<ReviewerQueueItemResponse>>> GetMentorQueue(
        CancellationToken cancellationToken)
    {
        var mentorId = GetCurrentUserId();
        return Ok(await BuildReviewerQueueAsync(mentorId, cancellationToken));
    }

    [Authorize(Roles = UserRoles.AcademicCounselor)]
    [HttpGet("api/counselor/roadmap-review-queue")]
    public async Task<ActionResult<IReadOnlyList<ReviewerQueueItemResponse>>> GetCounselorQueue(
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();
        return Ok(await BuildReviewerQueueAsync(counselorId, cancellationToken));
    }

    [Authorize(Roles = $"{UserRoles.IndustryMentor},{UserRoles.AcademicCounselor},{UserRoles.Admin}")]
    [HttpPost("api/roadmap-node/review-requests/{requestId:guid}/approve")]
    public async Task<ActionResult<RoadmapReviewRequestResponse>> ApproveRequest(
        Guid requestId,
        ApproveOrRejectRequest request,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetCurrentUserId();
        var reviewRequest = await dbContext.RoadmapNodeReviewRequests
            .Include(item => item.RoadmapNode)
            .ThenInclude(node => node.Roadmap)
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (reviewRequest is null)
        {
            return NotFound(new { message = "Review request not found." });
        }

        if (reviewRequest.ReviewerId != reviewerId && !User.IsInRole(UserRoles.Admin))
        {
            return Forbid();
        }

        if (reviewRequest.Status != "Pending")
        {
            return BadRequest(new { message = "Only pending requests can be approved." });
        }

        var now = DateTimeOffset.UtcNow;
        reviewRequest.Status = "Approved";
        reviewRequest.ReviewerNote = request.ReviewerNote?.Trim();
        reviewRequest.RespondedAt = now;

        var node = reviewRequest.RoadmapNode;
        node.Status = "Verified";
        node.UpdatedAt = now;

        await RecalculateRoadmapProgressAsync(node, cancellationToken);

        // Notify student
        var reviewerName = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == reviewerId)
            .Select(user => user.FullName)
            .SingleOrDefaultAsync(cancellationToken);

        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = reviewRequest.StudentId,
            Type = "RoadmapReviewApproved",
            Title = "Yêu cầu review đã được duyệt",
            Message = $"{reviewerName ?? "Mentor"} đã verify module \"{node.Title}\".",
            LinkUrl = "#roadmap",
            CreatedAt = now,
            IsRead = false
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await ToResponseAsync(reviewRequest, cancellationToken));
    }

    [Authorize(Roles = $"{UserRoles.IndustryMentor},{UserRoles.AcademicCounselor},{UserRoles.Admin}")]
    [HttpPost("api/roadmap-node/review-requests/{requestId:guid}/reject")]
    public async Task<ActionResult<RoadmapReviewRequestResponse>> RejectRequest(
        Guid requestId,
        ApproveOrRejectRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReviewerNote))
        {
            return BadRequest(new { message = "Reviewer note is required when rejecting." });
        }

        var reviewerId = GetCurrentUserId();
        var reviewRequest = await dbContext.RoadmapNodeReviewRequests
            .Include(item => item.RoadmapNode)
            .ThenInclude(node => node.Roadmap)
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (reviewRequest is null)
        {
            return NotFound(new { message = "Review request not found." });
        }

        if (reviewRequest.ReviewerId != reviewerId && !User.IsInRole(UserRoles.Admin))
        {
            return Forbid();
        }

        if (reviewRequest.Status != "Pending")
        {
            return BadRequest(new { message = "Only pending requests can be rejected." });
        }

        var now = DateTimeOffset.UtcNow;
        reviewRequest.Status = "Rejected";
        reviewRequest.ReviewerNote = request.ReviewerNote.Trim();
        reviewRequest.RespondedAt = now;

        var node = reviewRequest.RoadmapNode;
        // Q4 = A: revert to Completed so student can resubmit
        node.Status = "Completed";
        node.UpdatedAt = now;

        await RecalculateRoadmapProgressAsync(node, cancellationToken);

        // Notify student
        var reviewerName = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == reviewerId)
            .Select(user => user.FullName)
            .SingleOrDefaultAsync(cancellationToken);

        dbContext.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = reviewRequest.StudentId,
            Type = "RoadmapReviewRejected",
            Title = "Yêu cầu review bị từ chối",
            Message = $"{reviewerName ?? "Mentor"} đã từ chối module \"{node.Title}\". Lý do: {reviewRequest.ReviewerNote}",
            LinkUrl = "#roadmap",
            CreatedAt = now,
            IsRead = false
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await ToResponseAsync(reviewRequest, cancellationToken));
    }

    // ============================================================
    //  HELPERS
    // ============================================================

    private async Task<IReadOnlyList<ReviewerQueueItemResponse>> BuildReviewerQueueAsync(
        Guid reviewerId,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.RoadmapNodeReviewRequests
            .AsNoTracking()
            .Where(item => item.ReviewerId == reviewerId)
            .OrderBy(item => item.Status == "Pending" ? 0 : 1)
            .ThenByDescending(item => item.RequestedAt)
            .Include(item => item.Student)
            .Include(item => item.RoadmapNode)
            .ThenInclude(node => node.Roadmap)
            .ThenInclude(roadmap => roadmap.CareerRole)
            .ToListAsync(cancellationToken);

        return items.Select(item => new ReviewerQueueItemResponse(
            item.Id,
            item.Status,
            item.RequestedAt,
            item.RespondedAt,
            item.StudentNote,
            item.ReviewerNote,
            item.EvidenceUrl,
            item.EvidenceType,
            item.EvidenceFileName,
            new ReviewerQueueStudentInfo(
                item.Student.Id,
                item.Student.FullName,
                item.Student.Email,
                item.Student.AvatarUrl),
            new ReviewerQueueNodeInfo(
                item.RoadmapNode.Id,
                item.RoadmapNode.Title,
                item.RoadmapNode.Description,
                item.RoadmapNode.NodeType,
                item.RoadmapNode.Status,
                item.RoadmapNode.Roadmap.Id,
                item.RoadmapNode.Roadmap.Title,
                item.RoadmapNode.Roadmap.CareerRole.Name))).ToList();
    }

    private async Task RecalculateRoadmapProgressAsync(RoadmapNode node, CancellationToken cancellationToken)
    {
        var roadmapNodes = await dbContext.RoadmapNodes
            .Where(item => item.RoadmapId == node.RoadmapId)
            .ToListAsync(cancellationToken);
        var progressNodes = roadmapNodes
            .Where(item => !item.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var completedCount = progressNodes.Count(item => item.Id == node.Id
            ? node.Status is "Completed" or "Verified"
            : item.Status is "Completed" or "Verified");

        node.Roadmap.Progress = progressNodes.Count == 0
            ? 0
            : Math.Round(completedCount * 100m / progressNodes.Count, 2);
        node.Roadmap.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private async Task<MentorQuotaInfo> CalculateMentorQuotaAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var activeSubscription = await dbContext.Subscriptions
            .AsNoTracking()
            .Include(item => item.Plan)
            .Where(item => item.UserId == studentId && item.Status == "Active")
            .OrderByDescending(item => item.StartedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var limit = FreePlanReviewLimit;
        var since = DateTimeOffset.MinValue;

        if (activeSubscription is not null)
        {
            since = activeSubscription.StartedAt ?? since;
            // try parse limit from plan features json
            // (mentor controller already handles this; we keep simple fallback)
        }

        var used = await dbContext.MentorFeedbacks
            .AsNoTracking()
            .CountAsync(item => item.StudentId == studentId && item.CreatedAt >= since, cancellationToken);

        return new MentorQuotaInfo(limit, used, Math.Max(limit - used, 0));
    }

    private async Task<RoadmapReviewRequestResponse> ToResponseAsync(
        RoadmapNodeReviewRequest item,
        CancellationToken cancellationToken)
    {
        var reviewer = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == item.ReviewerId)
            .Select(user => new { user.Id, user.FullName, user.Email, user.AvatarUrl })
            .SingleOrDefaultAsync(cancellationToken);

        return ToResponse(item, reviewer is null ? null : new User
        {
            Id = reviewer.Id,
            FullName = reviewer.FullName,
            Email = reviewer.Email,
            AvatarUrl = reviewer.AvatarUrl
        });
    }

    private static RoadmapReviewRequestResponse ToResponse(RoadmapNodeReviewRequest item, User? reviewer)
    {
        return new RoadmapReviewRequestResponse(
            item.Id,
            item.RoadmapNodeId,
            item.Status,
            item.ReviewerRole,
            item.StudentNote,
            item.ReviewerNote,
            item.EvidenceUrl,
            item.EvidenceType,
            item.EvidenceFileName,
            item.RequestedAt,
            item.RespondedAt,
            reviewer is null ? null : new ReviewerSummary(
                reviewer.Id,
                reviewer.FullName,
                reviewer.Email,
                reviewer.AvatarUrl));
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(value!);
    }

    private record MentorQuotaInfo(int Limit, int Used, int Remaining);
}

// ===== DTOs =====

public sealed record AvailableReviewerInfo(
    Guid UserId,
    string FullName,
    string Email,
    string? AvatarUrl,
    string Role,
    int? Quota,
    int? Used,
    int? Remaining,
    bool Available);

public sealed record AvailableReviewersResponse(
    AvailableReviewerInfo? Counselor,
    IReadOnlyList<AvailableReviewerInfo> IndustryMentors);

public sealed record CreateReviewRequestRequest(
    Guid ReviewerId,
    string? StudentNote,
    string? EvidenceUrl,
    string? EvidenceType,
    string? EvidenceFileName);

public sealed record ApproveOrRejectRequest(string? ReviewerNote);

public sealed record UploadEvidenceRequest(IFormFile File);

public sealed record EvidenceUploadResponse(
    string ObjectName,
    string FileName,
    long FileSize,
    string ContentType,
    string EvidenceType);

public sealed record ReviewerSummary(
    Guid UserId,
    string FullName,
    string Email,
    string? AvatarUrl);

public sealed record RoadmapReviewRequestResponse(
    Guid Id,
    Guid RoadmapNodeId,
    string Status,
    string ReviewerRole,
    string? StudentNote,
    string? ReviewerNote,
    string? EvidenceUrl,
    string? EvidenceType,
    string? EvidenceFileName,
    DateTimeOffset RequestedAt,
    DateTimeOffset? RespondedAt,
    ReviewerSummary? Reviewer);

public sealed record ReviewerQueueStudentInfo(
    Guid UserId,
    string FullName,
    string Email,
    string? AvatarUrl);

public sealed record ReviewerQueueNodeInfo(
    Guid NodeId,
    string Title,
    string? Description,
    string NodeType,
    string Status,
    Guid RoadmapId,
    string RoadmapTitle,
    string CareerRoleName);

public sealed record ReviewerQueueItemResponse(
    Guid Id,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? RespondedAt,
    string? StudentNote,
    string? ReviewerNote,
    string? EvidenceUrl,
    string? EvidenceType,
    string? EvidenceFileName,
    ReviewerQueueStudentInfo Student,
    ReviewerQueueNodeInfo Node);

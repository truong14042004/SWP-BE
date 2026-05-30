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
    IFileStorageService storageService,
    IEmailSender emailSender,
    IStudentReviewQuotaService quotaService,
    IAiReviewSummaryService aiReviewSummaryService,
    ILogger<RoadmapReviewController> logger) : ControllerBase
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

    // FreePlanReviewLimit removed — quota now computed by IStudentReviewQuotaService.

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
            return NotFound(new { message = "Không tìm thấy module lộ trình." });
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

        // Industry Mentors: tất cả mentor active, hiển thị quota chung (theo student)
        // Quota áp dụng cho student chứ không theo mentor — tính 1 lần.
        var sharedQuota = await quotaService.GetQuotaAsync(studentId, cancellationToken);
        var studentRanOutOfQuota = sharedQuota.Remaining <= 0;

        var mentors = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRoles.IndustryMentor && user.IsActive)
            .ToListAsync(cancellationToken);

        // Pull pending requests for this node một lần duy nhất (bỏ N+1).
        var pendingMentorIdsForNode = await dbContext.RoadmapNodeReviewRequests
            .AsNoTracking()
            .Where(item => item.RoadmapNodeId == id && item.Status == "Pending")
            .Select(item => item.ReviewerId)
            .ToListAsync(cancellationToken);
        var pendingMentorIdSet = pendingMentorIdsForNode.ToHashSet();

        var mentorInfos = mentors
            .Select(mentor => new AvailableReviewerInfo(
                mentor.Id,
                mentor.FullName,
                mentor.Email,
                mentor.AvatarUrl,
                UserRoles.IndustryMentor,
                Quota: sharedQuota.Limit,
                Used: sharedQuota.Used,
                Remaining: sharedQuota.Remaining,
                // Mentor khả dụng khi: student còn quota VÀ mentor này không đang có pending
                // request cho cùng node này.
                Available: !studentRanOutOfQuota && !pendingMentorIdSet.Contains(mentor.Id)))
            .ToList();

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
            return NotFound(new { message = "Không tìm thấy module lộ trình." });
        }

        // Group nodes: allow review only when all non-group descendants
        // are Completed or Verified (Design A — direct children only).
        var isGroup = node.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase);
        if (isGroup)
        {
            var children = await dbContext.RoadmapNodes
                .AsNoTracking()
                .Where(item => item.ParentNodeId == id)
                .ToListAsync(cancellationToken);

            if (children.Count == 0)
            {
                return BadRequest(new { message = "Nhóm module không có bài học con nào để đánh giá." });
            }

            var nonGroupChildren = children
                .Where(item => !item.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (nonGroupChildren.Count == 0)
            {
                return BadRequest(new
                {
                    message = "Nhóm này chỉ chứa các nhóm con. Hãy đánh giá các bài học bên trong trước."
                });
            }

            var pending = nonGroupChildren
                .Where(item => item.Status is not ("Completed" or "Verified"))
                .ToList();

            if (pending.Count > 0)
            {
                return BadRequest(new
                {
                    message = $"{pending.Count}/{nonGroupChildren.Count} module chưa hoàn thành. Hoàn thành tất cả module trong nhóm trước khi yêu cầu review nhóm."
                });
            }
        }
        else
        {
            if (node.Status is not ("Completed" or "NeedReview"))
            {
                return BadRequest(new { message = "Module phải được đánh dấu hoàn thành trước khi gửi yêu cầu đánh giá." });
            }
        }

        // Validate reviewer
        var reviewer = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == request.ReviewerId && user.IsActive, cancellationToken);

        if (reviewer is null)
        {
            return BadRequest(new { message = "Không tìm thấy người đánh giá." });
        }

        if (reviewer.Role != UserRoles.AcademicCounselor && reviewer.Role != UserRoles.IndustryMentor)
        {
            return BadRequest(new { message = "Người dùng được chọn không có quyền đánh giá lộ trình." });
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
                return BadRequest(new { message = "Cố vấn này chưa được phân công cho bạn." });
            }
        }

        // If mentor: check quota
        if (reviewer.Role == UserRoles.IndustryMentor)
        {
            var quota = await quotaService.GetQuotaAsync(studentId, cancellationToken);
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

        // Send email (fire-and-forget; do not break the flow if SMTP fails)
        _ = SendReviewEmailSafelyAsync(
            reviewer.Email,
            $"[CareerMap] Yêu cầu review mới từ {student?.FullName ?? "sinh viên"}",
            BuildReviewRequestEmail(
                reviewer.FullName,
                student?.FullName ?? "Sinh viên",
                node.Title,
                request.StudentNote,
                request.EvidenceUrl,
                request.EvidenceType));

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
            return NotFound(new { message = "Không tìm thấy yêu cầu review." });
        }

        if (reviewRequest.StudentId != studentId)
        {
            return Forbid();
        }

        if (reviewRequest.Status != "Pending")
        {
            return BadRequest(new { message = "Chỉ có thể hủy các yêu cầu đang ở trạng thái chờ duyệt." });
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
            return NotFound(new { message = "Không tìm thấy module lộ trình." });
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
            return BadRequest(new { message = "Vui lòng chọn tệp tin minh chứng." });
        }

        if (request.File.Length > MaxEvidenceFileSize)
        {
            return BadRequest(new { message = "Kích thước tệp tin vượt quá giới hạn 25 MB." });
        }

        var fileName = request.File.FileName;
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedEvidenceExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                message = $"Loại tệp {extension} không được chấp nhận. Vui lòng sử dụng các định dạng: {string.Join(", ", AllowedEvidenceExtensions)}."
            });
        }

        var contentType = request.File.ContentType?.ToLowerInvariant() ?? string.Empty;
        if (!AllowedEvidenceContentTypes.Contains(contentType))
        {
            // Allow octet-stream for zip uploaded by some browsers
            if (contentType != "application/octet-stream")
            {
                return BadRequest(new { message = $"Loại nội dung {contentType} không được phép tải lên." });
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
            return NotFound(new { message = "Không tìm thấy yêu cầu review." });
        }

        if (reviewRequest.ReviewerId != reviewerId && !User.IsInRole(UserRoles.Admin))
        {
            return Forbid();
        }

        if (reviewRequest.Status != "Pending")
        {
            return BadRequest(new { message = "Chỉ có thể duyệt các yêu cầu ở trạng thái chờ duyệt." });
        }

        var now = DateTimeOffset.UtcNow;
        reviewRequest.Status = "Approved";
        reviewRequest.ReviewerNote = request.ReviewerNote?.Trim();
        reviewRequest.RespondedAt = now;

        var node = reviewRequest.RoadmapNode;
        node.Status = "Verified";
        node.UpdatedAt = now;

        // Group review: cascade verify all non-group descendants that are
        // Completed (so progress reflects the group sign-off).
        if (node.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase))
        {
            var children = await dbContext.RoadmapNodes
                .Where(item => item.ParentNodeId == node.Id
                    && !item.NodeType.ToLower().Equals("group")
                    && item.Status != "Verified")
                .ToListAsync(cancellationToken);

            foreach (var child in children)
            {
                child.Status = "Verified";
                child.UpdatedAt = now;
            }
        }

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

        // Email student about approval
        var studentEmail = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == reviewRequest.StudentId)
            .Select(user => new { user.Email, user.FullName })
            .SingleOrDefaultAsync(cancellationToken);

        if (studentEmail is not null)
        {
            _ = SendReviewEmailSafelyAsync(
                studentEmail.Email,
                $"[CareerMap] Yêu cầu review đã được duyệt: {node.Title}",
                BuildReviewApprovedEmail(
                    studentEmail.FullName,
                    reviewerName ?? "Reviewer",
                    node.Title,
                    request.ReviewerNote));
        }

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
            return BadRequest(new { message = "Vui lòng nhập nhận xét/lý do khi từ chối." });
        }

        var reviewerId = GetCurrentUserId();
        var reviewRequest = await dbContext.RoadmapNodeReviewRequests
            .Include(item => item.RoadmapNode)
            .ThenInclude(node => node.Roadmap)
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (reviewRequest is null)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu review." });
        }

        if (reviewRequest.ReviewerId != reviewerId && !User.IsInRole(UserRoles.Admin))
        {
            return Forbid();
        }

        if (reviewRequest.Status != "Pending")
        {
            return BadRequest(new { message = "Chỉ có thể từ chối các yêu cầu ở trạng thái chờ duyệt." });
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

        // Email student about rejection
        var studentEmail = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == reviewRequest.StudentId)
            .Select(user => new { user.Email, user.FullName })
            .SingleOrDefaultAsync(cancellationToken);

        if (studentEmail is not null)
        {
            _ = SendReviewEmailSafelyAsync(
                studentEmail.Email,
                $"[CareerMap] Yêu cầu review bị từ chối: {node.Title}",
                BuildReviewRejectedEmail(
                    studentEmail.FullName,
                    reviewerName ?? "Reviewer",
                    node.Title,
                    reviewRequest.ReviewerNote!));
        }

        return Ok(await ToResponseAsync(reviewRequest, cancellationToken));
    }

    // ============================================================
    //  EVIDENCE — Signed URL for reviewers to download
    // ============================================================

    [Authorize(Roles = $"{UserRoles.IndustryMentor},{UserRoles.AcademicCounselor},{UserRoles.Admin}")]
    [HttpGet("api/roadmap-node/review-requests/{requestId:guid}/evidence-url")]
    public async Task<ActionResult<EvidenceDownloadUrlResponse>> GetEvidenceDownloadUrl(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetCurrentUserId();
        var reviewRequest = await dbContext.RoadmapNodeReviewRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (reviewRequest is null)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu review." });
        }

        if (reviewRequest.ReviewerId != reviewerId && !User.IsInRole(UserRoles.Admin))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(reviewRequest.EvidenceUrl))
        {
            return BadRequest(new { message = "Yêu cầu này không đính kèm tệp minh chứng." });
        }

        // External git URL or absolute URL is already public
        if (reviewRequest.EvidenceType == "GitRepository"
            || reviewRequest.EvidenceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || reviewRequest.EvidenceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new EvidenceDownloadUrlResponse(
                reviewRequest.EvidenceUrl,
                reviewRequest.EvidenceFileName,
                ExpiresAt: null));
        }

        // Storage object: generate signed read URL valid for 15 minutes
        var expiresIn = TimeSpan.FromMinutes(15);
        try
        {
            var url = await storageService.CreateSignedReadUrlAsync(
                reviewRequest.EvidenceUrl,
                expiresIn,
                cancellationToken,
                downloadFileName: reviewRequest.EvidenceFileName);

            return Ok(new EvidenceDownloadUrlResponse(
                url,
                reviewRequest.EvidenceFileName,
                ExpiresAt: DateTimeOffset.UtcNow.Add(expiresIn)));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Không tạo được link tải evidence.",
                detail = ex.Message
            });
        }
    }

    // ============================================================
    //  AI REVIEW SUMMARY (mentor on-demand)
    // ============================================================

    [Authorize(Roles = $"{UserRoles.IndustryMentor},{UserRoles.Admin}")]
    [HttpPost("api/roadmap-node/review-requests/{requestId:guid}/ai-summary")]
    public async Task<ActionResult<AiReviewSummaryResponse>> GenerateAiSummary(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetCurrentUserId();

        var request = await dbContext.RoadmapNodeReviewRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (request is null)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu review." });
        }

        if (request.ReviewerId != reviewerId && !User.IsInRole(UserRoles.Admin))
        {
            return Forbid();
        }

        try
        {
            var summary = await aiReviewSummaryService.GenerateAsync(requestId, reviewerId, cancellationToken);
            return Ok(ToAiSummaryResponse(summary));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = $"{UserRoles.IndustryMentor},{UserRoles.AcademicCounselor},{UserRoles.Admin}")]
    [HttpGet("api/roadmap-node/review-requests/{requestId:guid}/ai-summary")]
    public async Task<ActionResult<AiReviewSummaryResponse>> GetAiSummary(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetCurrentUserId();

        var request = await dbContext.RoadmapNodeReviewRequests
            .AsNoTracking()
            .Include(item => item.AiSummary)
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);

        if (request is null)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu review." });
        }

        if (request.ReviewerId != reviewerId && !User.IsInRole(UserRoles.Admin))
        {
            return Forbid();
        }

        if (request.AiSummary is null)
        {
            return NoContent();
        }

        return Ok(ToAiSummaryResponse(request.AiSummary));
    }

    private static AiReviewSummaryResponse ToAiSummaryResponse(AiReviewSummary summary)
    {
        return new AiReviewSummaryResponse(
            summary.Id,
            summary.ReviewRequestId,
            summary.EvidenceType,
            summary.EvidenceUrl,
            summary.Model,
            summary.TokensUsed,
            ParseStringArray(summary.TechStackJson),
            ParseStringArray(summary.StrengthsJson),
            ParseStringArray(summary.WeaknessesJson),
            ParseStringArray(summary.SuggestedQuestionsJson),
            ParseSkillMapping(summary.SkillMappingJson),
            summary.OverallSummary,
            summary.GeneratedAt);
    }

    private static IReadOnlyList<string> ParseStringArray(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                return [];
            }

            var list = new List<string>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var value = element.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        list.Add(value);
                    }
                }
            }

            return list;
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private static AiReviewSkillMapping? ParseSkillMapping(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return null;
            }

            var root = doc.RootElement;
            bool? matches = null;
            if (root.TryGetProperty("matchesNode", out var matchesElement)
                && (matchesElement.ValueKind == System.Text.Json.JsonValueKind.True
                    || matchesElement.ValueKind == System.Text.Json.JsonValueKind.False))
            {
                matches = matchesElement.GetBoolean();
            }

            string? reason = null;
            if (root.TryGetProperty("reason", out var reasonElement)
                && reasonElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                reason = reasonElement.GetString();
            }

            var missing = new List<string>();
            if (root.TryGetProperty("missingAspects", out var missingElement)
                && missingElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var element in missingElement.EnumerateArray())
                {
                    if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var value = element.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            missing.Add(value);
                        }
                    }
                }
            }

            return new AiReviewSkillMapping(matches, reason, missing);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
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

        var studentIds = items.Select(item => item.StudentId).Distinct().ToList();
        var profiles = await dbContext.StudentProfiles
            .AsNoTracking()
            .Where(p => studentIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken);

        return items.Select(item => {
            profiles.TryGetValue(item.StudentId, out var profile);
            return new ReviewerQueueItemResponse(
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
                    item.Student.AvatarUrl,
                    profile?.CvUrl,
                    profile?.CvName),
                new ReviewerQueueNodeInfo(
                    item.RoadmapNode.Id,
                    item.RoadmapNode.Title,
                    item.RoadmapNode.Description,
                    item.RoadmapNode.NodeType,
                    item.RoadmapNode.Status,
                    item.RoadmapNode.Roadmap?.Id ?? Guid.Empty,
                    item.RoadmapNode.Roadmap?.Title ?? "Unknown Roadmap",
                    item.RoadmapNode.Roadmap?.CareerRole?.Name ?? "Unknown Role"));
        }).ToList();
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

    // CalculateMentorQuotaAsync was removed; use IStudentReviewQuotaService instead.

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
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Mã xác thực người dùng không hợp lệ.");
    }

    private async Task SendReviewEmailSafelyAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await emailSender.SendAsync(toEmail, subject, body, cts.Token);
        }
        catch (Exception ex)
        {
            // Email is best-effort. Log and continue.
            logger.LogWarning(ex, "Failed to send review notification email to {Email}", toEmail);
        }
    }

    private static string BuildReviewRequestEmail(
        string reviewerName,
        string studentName,
        string nodeTitle,
        string? studentNote,
        string? evidenceUrl,
        string? evidenceType) =>
        $"""
        <div style="font-family:system-ui,-apple-system,sans-serif;max-width:560px;margin:0 auto;color:#1d1d1f">
          <h2 style="color:#0066cc;margin:0 0 12px">Yêu cầu review mới</h2>
          <p>Chào {WebEncode(reviewerName)},</p>
          <p><strong>{WebEncode(studentName)}</strong> vừa gửi yêu cầu review cho module:</p>
          <blockquote style="margin:8px 0;padding:12px 16px;background:#fafafc;border-left:3px solid #0066cc;border-radius:6px">
            {WebEncode(nodeTitle)}
          </blockquote>
          {(string.IsNullOrWhiteSpace(studentNote) ? "" : $"<p><strong>Ghi chú:</strong> {WebEncode(studentNote)}</p>")}
          {(string.IsNullOrWhiteSpace(evidenceUrl) ? "" : $"<p><strong>Evidence:</strong> {WebEncode(evidenceType ?? "File")}</p>")}
          <p style="margin-top:24px">Mở CareerMap để xem chi tiết và duyệt yêu cầu.</p>
          <hr style="border:0;border-top:1px solid #eee;margin:24px 0">
          <small style="color:#8a8a8e">Email tự động từ CareerMap. Vui lòng không trả lời trực tiếp.</small>
        </div>
        """;

    private static string BuildReviewApprovedEmail(
        string studentName,
        string reviewerName,
        string nodeTitle,
        string? reviewerNote) =>
        $"""
        <div style="font-family:system-ui,-apple-system,sans-serif;max-width:560px;margin:0 auto;color:#1d1d1f">
          <h2 style="color:#1f5e2c;margin:0 0 12px">✓ Yêu cầu review đã được duyệt</h2>
          <p>Chào {WebEncode(studentName)},</p>
          <p><strong>{WebEncode(reviewerName)}</strong> đã verify module:</p>
          <blockquote style="margin:8px 0;padding:12px 16px;background:rgba(52,199,89,0.08);border-left:3px solid #34c759;border-radius:6px">
            {WebEncode(nodeTitle)}
          </blockquote>
          {(string.IsNullOrWhiteSpace(reviewerNote) ? "" : $"<p><strong>Nhận xét từ reviewer:</strong> {WebEncode(reviewerNote)}</p>")}
          <p style="margin-top:24px">Module này đã được đánh dấu Verified trong roadmap của bạn.</p>
          <hr style="border:0;border-top:1px solid #eee;margin:24px 0">
          <small style="color:#8a8a8e">Email tự động từ CareerMap.</small>
        </div>
        """;

    private static string BuildReviewRejectedEmail(
        string studentName,
        string reviewerName,
        string nodeTitle,
        string reviewerNote) =>
        $"""
        <div style="font-family:system-ui,-apple-system,sans-serif;max-width:560px;margin:0 auto;color:#1d1d1f">
          <h2 style="color:#a30005;margin:0 0 12px">Yêu cầu review bị từ chối</h2>
          <p>Chào {WebEncode(studentName)},</p>
          <p><strong>{WebEncode(reviewerName)}</strong> đã từ chối yêu cầu review cho module:</p>
          <blockquote style="margin:8px 0;padding:12px 16px;background:rgba(255,59,48,0.06);border-left:3px solid #ff3b30;border-radius:6px">
            {WebEncode(nodeTitle)}
          </blockquote>
          <p><strong>Lý do:</strong> {WebEncode(reviewerNote)}</p>
          <p style="margin-top:24px">Module được đưa về trạng thái Completed. Bạn có thể cập nhật evidence và gửi lại yêu cầu mới.</p>
          <hr style="border:0;border-top:1px solid #eee;margin:24px 0">
          <small style="color:#8a8a8e">Email tự động từ CareerMap.</small>
        </div>
        """;

    private static string WebEncode(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    // MentorQuotaInfo removed; superseded by SWP_BE.Services.StudentReviewQuota.
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

public sealed record EvidenceDownloadUrlResponse(
    string DownloadUrl,
    string? FileName,
    DateTimeOffset? ExpiresAt);

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
    string? AvatarUrl,
    string? CvUrl = null,
    string? CvName = null);

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

public sealed record AiReviewSkillMapping(
    bool? MatchesNode,
    string? Reason,
    IReadOnlyList<string> MissingAspects);

public sealed record AiReviewSummaryResponse(
    Guid Id,
    Guid ReviewRequestId,
    string EvidenceType,
    string EvidenceUrl,
    string? Model,
    int? TokensUsed,
    IReadOnlyList<string> TechStack,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<string> SuggestedQuestions,
    AiReviewSkillMapping? SkillMapping,
    string? OverallSummary,
    DateTimeOffset GeneratedAt);

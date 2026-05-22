using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.AcademicCounselor)]
[Route("api/counselor")]
public sealed class CounselorController(AppDbContext dbContext) : ControllerBase
{
    // GET /api/counselor/students
    // Lấy danh sách sinh viên active được phân công cho cố vấn đang đăng nhập, kèm target role và match score gần nhất
    [HttpGet("students")]
    [ProducesResponseType<IReadOnlyList<CounselorStudentSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CounselorStudentSummaryResponse>>> GetStudents(
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();

        var assignments = await dbContext.CounselorAssignments
            .AsNoTracking()
            .Where(a => a.CounselorId == counselorId && a.Status == "Active")
            .Select(a => new { a.StudentId, AssignedAt = a.CreatedAt })
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return Ok(Array.Empty<CounselorStudentSummaryResponse>());
        }

        var assignedStudentIds = assignments.Select(a => a.StudentId).ToList();
        var assignmentDateByStudent = assignments
            .GroupBy(a => a.StudentId)
            .ToDictionary(g => g.Key, g => g.Max(a => a.AssignedAt));

        // Lấy thông tin user cơ bản
        var students = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRoles.Student && user.IsActive && assignedStudentIds.Contains(user.Id))
            .OrderBy(user => user.FullName)
            .Select(user => new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Username,
                user.AvatarUrl,
                user.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var studentIds = students.Select(s => s.Id).ToList();

        // Lấy target role của từng sinh viên (1 query)
        var profiles = await dbContext.StudentProfiles
            .AsNoTracking()
            .Include(p => p.TargetRole)
            .Where(p => studentIds.Contains(p.UserId))
            .Select(p => new
            {
                p.UserId,
                p.TargetRoleId,
                TargetRoleName = p.TargetRole == null ? null : p.TargetRole.Name
            })
            .ToDictionaryAsync(p => p.UserId, cancellationToken);

        // Lấy skill gap report mới nhất của từng sinh viên (1 query, group + max)
        var latestGaps = await dbContext.SkillGapReports
            .AsNoTracking()
            .Where(report => studentIds.Contains(report.UserId))
            .GroupBy(report => report.UserId)
            .Select(group => group
                .OrderByDescending(report => report.CreatedAt)
                .Select(report => new
                {
                    report.UserId,
                    report.MatchScore,
                    report.CreatedAt
                })
                .First())
            .ToDictionaryAsync(report => report.UserId, cancellationToken);

        var result = students.Select(student =>
        {
            profiles.TryGetValue(student.Id, out var profile);
            latestGaps.TryGetValue(student.Id, out var gap);
            assignmentDateByStudent.TryGetValue(student.Id, out var assignedAt);

            return new CounselorStudentSummaryResponse(
                student.Id,
                student.FullName,
                student.Email,
                student.Username,
                student.AvatarUrl,
                student.CreatedAt,
                assignedAt == default ? (DateTimeOffset?)null : assignedAt,
                profile?.TargetRoleId,
                profile?.TargetRoleName,
                gap?.MatchScore,
                gap?.CreatedAt);
        }).ToList();

        return Ok(result);
    }

    // GET /api/counselor/students/{studentId}/profile
    // Lấy profile chi tiết của một sinh viên (yêu cầu thuộc phân công)
    [HttpGet("students/{studentId:guid}/profile")]
    [ProducesResponseType<CounselorStudentProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CounselorStudentProfileResponse>> GetStudentProfile(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();
        if (!await IsStudentAssignedToCounselorAsync(studentId, counselorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Student is not assigned to this counselor." });
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "Student was not found." });
        }

        var profile = await dbContext.StudentProfiles
            .AsNoTracking()
            .Include(p => p.TargetRole)
            .SingleOrDefaultAsync(p => p.UserId == studentId, cancellationToken);

        return Ok(new CounselorStudentProfileResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Username,
            user.AvatarUrl,
            user.CreatedAt,
            profile is null ? null : new CounselorProfileDetailsResponse(
                profile.Id,
                profile.School,
                profile.Major,
                profile.Year,
                profile.Gpa,
                profile.TargetRoleId,
                profile.TargetRole?.Name,
                profile.GithubUsername,
                profile.CareerGoal,
                profile.PreferredLearningHoursPerWeek,
                profile.CvUrl,
                profile.CvName,
                profile.CreatedAt,
                profile.UpdatedAt)));
    }

    // GET /api/counselor/students/{studentId}/skills
    // Lấy danh sách kỹ năng của sinh viên (yêu cầu thuộc phân công)
    [HttpGet("students/{studentId:guid}/skills")]
    [ProducesResponseType<IReadOnlyList<CounselorStudentSkillResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CounselorStudentSkillResponse>>> GetStudentSkills(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();
        if (!await IsStudentAssignedToCounselorAsync(studentId, counselorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Student is not assigned to this counselor." });
        }

        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Student was not found." });
        }

        var skills = await dbContext.UserSkills
            .AsNoTracking()
            .Include(us => us.Skill)
            .Include(us => us.VerifiedByUser)
            .Where(us => us.UserId == studentId)
            .OrderBy(us => us.Skill.Category)
            .ThenBy(us => us.Skill.Name)
            .Select(us => new CounselorStudentSkillResponse(
                us.Id,
                us.SkillId,
                us.Skill.Name,
                us.Skill.Category,
                us.Level,
                us.IsVerified,
                us.VerifiedByUserId,
                us.VerifiedByUser != null ? us.VerifiedByUser.FullName : null,
                us.EvidenceUrl,
                us.EvidenceType,
                us.CreatedAt,
                us.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(skills);
    }

    // GET /api/counselor/students/{studentId}/skill-gap
    // GET /api/counselor/students/{studentId}/skill-gap/latest
    // Lấy báo cáo skill gap gần nhất của sinh viên (yêu cầu thuộc phân công)
    [HttpGet("students/{studentId:guid}/skill-gap")]
    [HttpGet("students/{studentId:guid}/skill-gap/latest")]
    [ProducesResponseType<CounselorSkillGapReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CounselorSkillGapReportResponse>> GetStudentSkillGapLatest(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();
        if (!await IsStudentAssignedToCounselorAsync(studentId, counselorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Student is not assigned to this counselor." });
        }

        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Student was not found." });
        }

        var report = await dbContext.SkillGapReports
            .AsNoTracking()
            .Include(r => r.CareerRole)
            .Where(r => r.UserId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (report is null)
        {
            return NotFound(new { message = "No skill gap report found for this student." });
        }

        return await GetSkillGapReportInternalAsync(report, cancellationToken);
    }

    // GET /api/counselor/students/{studentId}/skill-gaps
    // Lấy lịch sử danh sách báo cáo skill gap của sinh viên (yêu cầu thuộc phân công)
    [HttpGet("students/{studentId:guid}/skill-gaps")]
    [ProducesResponseType<IReadOnlyList<CounselorSkillGapHistoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CounselorSkillGapHistoryResponse>>> GetStudentSkillGapsHistory(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();
        if (!await IsStudentAssignedToCounselorAsync(studentId, counselorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Student is not assigned to this counselor." });
        }

        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Student was not found." });
        }

        var reports = await dbContext.SkillGapReports
            .AsNoTracking()
            .Include(r => r.CareerRole)
            .Where(r => r.UserId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new CounselorSkillGapHistoryResponse(
                r.Id,
                r.UserId,
                r.CareerRoleId,
                r.CareerRole.Name,
                r.MatchScore,
                r.Summary,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(reports);
    }

    // GET /api/counselor/students/{studentId}/skill-gap/{reportId}
    // Xem một báo cáo skill gap cụ thể của sinh viên (yêu cầu thuộc phân công)
    [HttpGet("students/{studentId:guid}/skill-gap/{reportId:guid}")]
    [ProducesResponseType<CounselorSkillGapReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CounselorSkillGapReportResponse>> GetStudentSkillGapById(
        Guid studentId,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();
        if (!await IsStudentAssignedToCounselorAsync(studentId, counselorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Student is not assigned to this counselor." });
        }

        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Student was not found." });
        }

        var report = await dbContext.SkillGapReports
            .AsNoTracking()
            .Include(r => r.CareerRole)
            .SingleOrDefaultAsync(r => r.Id == reportId && r.UserId == studentId, cancellationToken);

        if (report is null)
        {
            return NotFound(new { message = "Skill gap report was not found for this student." });
        }

        return await GetSkillGapReportInternalAsync(report, cancellationToken);
    }

    // GET /api/counselor/students/{studentId}/roadmap
    // Lấy roadmap gần nhất của sinh viên (yêu cầu thuộc phân công) với đầy đủ thông tin cha-con & tài nguyên
    [HttpGet("students/{studentId:guid}/roadmap")]
    [ProducesResponseType<RoadmapResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoadmapResponse>> GetStudentRoadmap(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();
        if (!await IsStudentAssignedToCounselorAsync(studentId, counselorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Student is not assigned to this counselor." });
        }

        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Student was not found." });
        }

        var roadmap = await dbContext.Roadmaps
            .AsNoTracking()
            .Include(r => r.CareerRole)
            .Where(r => r.UserId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (roadmap is null)
        {
            return NotFound(new { message = "No roadmap found for this student." });
        }

        var nodes = await dbContext.RoadmapNodes
            .AsNoTracking()
            .Include(node => node.LearningResource)
            .ThenInclude(resource => resource!.Skill)
            .Include(node => node.Resources)
            .ThenInclude(item => item.LearningResource)
            .ThenInclude(resource => resource.Skill)
            .Where(node => node.RoadmapId == roadmap.Id)
            .OrderBy(node => node.OrderIndex)
            .ToListAsync(cancellationToken);

        return Ok(ToRoadmapResponse(roadmap, roadmap.CareerRole.Name, nodes));
    }

    // POST /api/counselor/feedback
    // Tạo feedback đầy đủ nghiệp vụ cho sinh viên (yêu cầu thuộc phân công)
    [HttpPost("feedback")]
    [ProducesResponseType<CounselorFeedbackResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CounselorFeedbackResponse>> CreateFeedback(
        CreateCounselorFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FeedbackText))
        {
            return BadRequest(new { message = "FeedbackText is required." });
        }

        if (request.Rating is < 1 or > 5)
        {
            return BadRequest(new { message = "Rating must be between 1 and 5." });
        }

        var counselorId = GetCurrentUserId();

        // 1. Xác thực phân công cố vấn - sinh viên
        if (!await IsStudentAssignedToCounselorAsync(request.StudentId, counselorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Student is not assigned to this counselor." });
        }

        // 2. Xác nhận sinh viên tồn tại
        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == request.StudentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Student was not found." });
        }

        // 3. Xác nhận roadmapId nếu được cung cấp
        if (request.RoadmapId is not null)
        {
            var roadmapExists = await dbContext.Roadmaps
                .AnyAsync(r => r.Id == request.RoadmapId && r.UserId == request.StudentId, cancellationToken);
            if (!roadmapExists)
            {
                return BadRequest(new { message = "Roadmap does not belong to this student." });
            }
        }

        // 4. Xác nhận skillGapReportId nếu được cung cấp
        if (request.SkillGapReportId is not null)
        {
            var reportExists = await dbContext.SkillGapReports
                .AnyAsync(r => r.Id == request.SkillGapReportId && r.UserId == request.StudentId, cancellationToken);
            if (!reportExists)
            {
                return BadRequest(new { message = "Skill gap report does not belong to this student." });
            }
        }

        var now = DateTimeOffset.UtcNow;
        var feedback = new CounselorFeedback
        {
            Id = Guid.NewGuid(),
            CounselorId = counselorId,
            StudentId = request.StudentId,
            RoadmapId = request.RoadmapId,
            SkillGapReportId = request.SkillGapReportId,
            FeedbackText = request.FeedbackText.Trim(),
            Rating = request.Rating,
            Recommendations = request.Recommendations?.Trim(),
            PrivateNotes = request.PrivateNotes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.CounselorFeedbacks.Add(feedback);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Load navigation properties for response
        await dbContext.Entry(feedback).Reference(f => f.Student).LoadAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetMyFeedbacks),
            ToFeedbackResponse(feedback));
    }

    // GET /api/counselor/feedback
    // Lấy danh sách các feedback mà cố vấn đang đăng nhập tự viết
    [HttpGet("feedback")]
    [ProducesResponseType<IReadOnlyList<CounselorFeedbackResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CounselorFeedbackResponse>>> GetMyFeedbacks(
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();

        var feedbacks = await dbContext.CounselorFeedbacks
            .AsNoTracking()
            .Include(f => f.Student)
            .Where(f => f.CounselorId == counselorId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(feedbacks.Select(f => ToFeedbackResponse(f)).ToList());
    }

    // GET /api/counselor/students/{studentId}/feedback
    // Lấy danh sách feedback mà cố vấn đang đăng nhập đã gửi riêng cho sinh viên này (yêu cầu thuộc phân công)
    [HttpGet("students/{studentId:guid}/feedback")]
    [ProducesResponseType<IReadOnlyList<CounselorFeedbackResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CounselorFeedbackResponse>>> GetFeedbacksForStudent(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();
        if (!await IsStudentAssignedToCounselorAsync(studentId, counselorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Student is not assigned to this counselor." });
        }

        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Student was not found." });
        }

        var feedbacks = await dbContext.CounselorFeedbacks
            .AsNoTracking()
            .Include(f => f.Student)
            .Where(f => f.CounselorId == counselorId && f.StudentId == studentId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(feedbacks.Select(f => ToFeedbackResponse(f)).ToList());
    }

    // ── Helper Methods ──────────────────────────────────────────────────────────

    private async Task<bool> IsStudentAssignedToCounselorAsync(Guid studentId, Guid counselorId, CancellationToken cancellationToken)
    {
        return await dbContext.CounselorAssignments
            .AnyAsync(a => a.CounselorId == counselorId && a.StudentId == studentId && a.Status == "Active", cancellationToken);
    }

    private Guid GetCurrentUserId()
    {
        var nameIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
        {
            throw new InvalidOperationException("User ID is missing or invalid in JWT token.");
        }
        return userId;
    }

    private async Task<ActionResult<CounselorSkillGapReportResponse>> GetSkillGapReportInternalAsync(
        SkillGapReport report,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.SkillGapReportItems
            .AsNoTracking()
            .Include(i => i.Skill)
            .Where(i => i.SkillGapReportId == report.Id)
            .OrderBy(i => i.Priority)
            .ThenBy(i => i.Skill.Name)
            .ToListAsync(cancellationToken);

        return Ok(new CounselorSkillGapReportResponse(
            report.Id,
            report.UserId,
            report.CareerRoleId,
            report.CareerRole.Name,
            report.MatchScore,
            report.Summary,
            report.CreatedAt,
            items.Select(i => new CounselorSkillGapReportItemResponse(
                i.SkillId,
                i.Skill.Name,
                i.Skill.Category,
                i.CurrentLevel,
                i.RequiredLevel,
                i.Status,
                i.Priority,
                i.Recommendation
            )).ToList()
        ));
    }

    private static CounselorFeedbackResponse ToFeedbackResponse(CounselorFeedback feedback) =>
        new(
            feedback.Id,
            feedback.CounselorId,
            feedback.StudentId,
            feedback.Student?.FullName,
            feedback.Student?.Email,
            feedback.RoadmapId,
            feedback.SkillGapReportId,
            feedback.FeedbackText,
            feedback.Rating,
            feedback.Recommendations,
            feedback.PrivateNotes,
            feedback.CreatedAt,
            feedback.UpdatedAt);

    // ── Mappers for RoadmapResponse ──────────────────────────────────────────

    private static RoadmapResponse ToRoadmapResponse(Roadmap roadmap, string careerRoleName, IReadOnlyList<RoadmapNode> nodes) =>
        new(
            roadmap.Id,
            roadmap.CareerRoleId,
            careerRoleName,
            roadmap.SkillGapReportId,
            roadmap.Title,
            roadmap.Description,
            roadmap.Status,
            roadmap.Progress,
            roadmap.CreatedAt,
            roadmap.UpdatedAt,
            nodes.OrderBy(node => node.OrderIndex).Select(node => ToNodeResponse(node)).ToList(),
            BuildNodeTree(nodes));

    private static IReadOnlyList<RoadmapNodeResponse> BuildNodeTree(IReadOnlyList<RoadmapNode> nodes)
    {
        var nodesByParent = nodes
            .OrderBy(node => node.OrderIndex)
            .GroupBy(node => node.ParentNodeId ?? Guid.Empty)
            .ToDictionary(group => group.Key, group => group.ToList());

        return BuildNodeChildren(Guid.Empty, nodesByParent);
    }

    private static IReadOnlyList<RoadmapNodeResponse> BuildNodeChildren(
        Guid parentNodeId,
        IReadOnlyDictionary<Guid, List<RoadmapNode>> nodesByParent)
    {
        if (!nodesByParent.TryGetValue(parentNodeId, out var children))
        {
            return [];
        }

        return children
            .OrderBy(node => node.OrderIndex)
            .Select(node => ToNodeResponse(node, BuildNodeChildren(node.Id, nodesByParent)))
            .ToList();
    }

    private static RoadmapNodeResponse ToNodeResponse(
        RoadmapNode node,
        IReadOnlyList<RoadmapNodeResponse>? children = null)
    {
        var learningResources = node.Resources
            .OrderBy(item => item.OrderIndex)
            .Select(item => ToLearningResourceResponse(item.LearningResource))
            .ToList();
        var primaryLearningResource = learningResources.FirstOrDefault()
            ?? (node.LearningResource is null ? null : ToLearningResourceResponse(node.LearningResource));

        return new(
            node.Id,
            node.SkillId,
            node.LearningResourceId,
            node.PrerequisiteNodeId,
            node.ParentNodeId,
            node.Title,
            node.Description,
            node.NodeType,
            node.Status,
            node.Level,
            node.OrderIndex,
            node.EstimatedHours,
            node.Priority,
            primaryLearningResource,
            learningResources,
            children ?? []);
    }

    private static RoadmapLearningResourceResponse ToLearningResourceResponse(LearningResource resource) =>
        new(
            resource.Id,
            resource.SkillId,
            resource.Skill?.Name,
            resource.Title,
            resource.Url,
            resource.StorageObjectName is null ? "Link" : "File",
            resource.ContentType,
            resource.FileSize,
            resource.ResourceType,
            resource.Difficulty,
            resource.EstimatedHours);
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

public sealed record CreateCounselorFeedbackRequest(
    Guid StudentId,
    string FeedbackText,
    Guid? RoadmapId,
    Guid? SkillGapReportId,
    int? Rating,
    string? Recommendations,
    string? PrivateNotes);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public sealed record CounselorStudentSummaryResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Username,
    string? AvatarUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AssignedAt,
    Guid? TargetRoleId,
    string? TargetRoleName,
    decimal? LatestMatchScore,
    DateTimeOffset? LatestSkillGapAt);

public sealed record CounselorStudentProfileResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Username,
    string? AvatarUrl,
    DateTimeOffset UserCreatedAt,
    CounselorProfileDetailsResponse? Profile);

public sealed record CounselorProfileDetailsResponse(
    Guid Id,
    string? School,
    string? Major,
    int? Year,
    decimal? Gpa,
    Guid? TargetRoleId,
    string? TargetRoleName,
    string? GithubUsername,
    string? CareerGoal,
    int? PreferredLearningHoursPerWeek,
    string? CvUrl,
    string? CvName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CounselorStudentSkillResponse(
    Guid Id,
    Guid SkillId,
    string SkillName,
    string SkillCategory,
    string Level,
    bool IsVerified,
    Guid? VerifiedByUserId,
    string? VerifiedByFullName,
    string? EvidenceUrl,
    string? EvidenceType,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CounselorFeedbackResponse(
    Guid Id,
    Guid CounselorId,
    Guid StudentId,
    string? StudentFullName,
    string? StudentEmail,
    Guid? RoadmapId,
    Guid? SkillGapReportId,
    string FeedbackText,
    int? Rating,
    string? Recommendations,
    string? PrivateNotes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CounselorSkillGapHistoryResponse(
    Guid Id,
    Guid UserId,
    Guid CareerRoleId,
    string CareerRoleName,
    decimal MatchScore,
    string? Summary,
    DateTimeOffset CreatedAt);

public sealed record CounselorSkillGapReportResponse(
    Guid Id,
    Guid UserId,
    Guid CareerRoleId,
    string CareerRoleName,
    decimal MatchScore,
    string? Summary,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CounselorSkillGapReportItemResponse> Items);

public sealed record CounselorSkillGapReportItemResponse(
    Guid SkillId,
    string SkillName,
    string SkillCategory,
    string CurrentLevel,
    string RequiredLevel,
    string Status,
    int Priority,
    string? Recommendation);

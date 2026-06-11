using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Services;

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
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sinh viên không được phân công cho cố vấn này." });
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "Không tìm thấy sinh viên." });
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
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sinh viên không được phân công cho cố vấn này." });
        }

        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Không tìm thấy sinh viên." });
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
                us.VerifiedLevel,
                us.VerificationStatus,
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
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sinh viên không được phân công cho cố vấn này." });
        }

        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Không tìm thấy sinh viên." });
        }

        var report = await dbContext.SkillGapReports
            .AsNoTracking()
            .Include(r => r.CareerRole)
            .Where(r => r.UserId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (report is null)
        {
            return NotFound(new { message = "Không tìm thấy báo cáo khoảng cách kỹ năng cho sinh viên này." });
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
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sinh viên không được phân công cho cố vấn này." });
        }

        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Không tìm thấy sinh viên." });
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
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sinh viên không được phân công cho cố vấn này." });
        }

        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Không tìm thấy sinh viên." });
        }

        var report = await dbContext.SkillGapReports
            .AsNoTracking()
            .Include(r => r.CareerRole)
            .SingleOrDefaultAsync(r => r.Id == reportId && r.UserId == studentId, cancellationToken);

        if (report is null)
        {
            return NotFound(new { message = "Không tìm thấy báo cáo khoảng cách kỹ năng cho sinh viên này." });
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
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sinh viên không được phân công cho cố vấn này." });
        }

        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Không tìm thấy sinh viên." });
        }

        var roadmap = await dbContext.Roadmaps
            .AsNoTracking()
            .Include(r => r.CareerRole)
            .Where(r => r.UserId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (roadmap is null)
        {
            return NotFound(new { message = "Không tìm thấy lộ trình cho sinh viên này." });
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

        var reqUpdate = await dbContext.RoleSkillRequirements
            .AsNoTracking()
            .Where(req => req.CareerRoleId == roadmap.CareerRoleId)
            .Select(req => (DateTimeOffset?)req.UpdatedAt)
            .MaxAsync(cancellationToken) ?? DateTimeOffset.MinValue;

        var roleUpdate = roadmap.CareerRole.UpdatedAt;
        var latestUpdate = reqUpdate > roleUpdate ? reqUpdate : roleUpdate;
        bool isOutdated = roadmap.CreatedAt < latestUpdate;

        return Ok(ToRoadmapResponse(roadmap, roadmap.CareerRole.Name, nodes, isOutdated));
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
            return BadRequest(new { message = "Nội dung phản hồi là bắt buộc." });
        }

        if (request.Rating is < 1 or > 5)
        {
            return BadRequest(new { message = "Đánh giá phải từ 1 đến 5 sao." });
        }

        var counselorId = GetCurrentUserId();

        // 1. Xác thực phân công cố vấn - sinh viên
        if (!await IsStudentAssignedToCounselorAsync(request.StudentId, counselorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sinh viên không được phân công cho cố vấn này." });
        }

        // 2. Xác nhận sinh viên tồn tại
        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == request.StudentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Không tìm thấy sinh viên." });
        }

        // 3. Xác nhận roadmapId nếu được cung cấp
        if (request.RoadmapId is not null)
        {
            var roadmapExists = await dbContext.Roadmaps
                .AnyAsync(r => r.Id == request.RoadmapId && r.UserId == request.StudentId, cancellationToken);
            if (!roadmapExists)
            {
                return BadRequest(new { message = "Lộ trình không thuộc về sinh viên này." });
            }
        }

        // 4. Xác nhận skillGapReportId nếu được cung cấp
        if (request.SkillGapReportId is not null)
        {
            var reportExists = await dbContext.SkillGapReports
                .AnyAsync(r => r.Id == request.SkillGapReportId && r.UserId == request.StudentId, cancellationToken);
            if (!reportExists)
            {
                return BadRequest(new { message = "Báo cáo khoảng cách kỹ năng không thuộc về sinh viên này." });
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
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sinh viên không được phân công cho cố vấn này." });
        }

        var studentExists = await dbContext.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRoles.Student, cancellationToken);

        if (!studentExists)
        {
            return NotFound(new { message = "Không tìm thấy sinh viên." });
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
            throw new InvalidOperationException("Mã người dùng bị thiếu hoặc không hợp lệ trong JWT token.");
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

    private static RoadmapResponse ToRoadmapResponse(Roadmap roadmap, string careerRoleName, IReadOnlyList<RoadmapNode> nodes, bool isOutdated) =>
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
            BuildNodeTree(nodes),
            isOutdated);

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
            resource.EstimatedHours,
            resource.LessonNumber);

    // GET /api/counselor/skill-verification-queue
    // Hàng đợi duyệt minh chứng kỹ năng
    [HttpGet("skill-verification-queue")]
    [ProducesResponseType<IReadOnlyList<SkillVerificationQueueItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SkillVerificationQueueItemResponse>>> GetSkillVerificationQueue(
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();

        var items = await dbContext.UserSkills
            .AsNoTracking()
            .Where(us => us.VerificationStatus == UserSkillVerificationStatus.PendingVerification
                && dbContext.CounselorAssignments.Any(a =>
                    a.CounselorId == counselorId
                    && a.StudentId == us.UserId
                    && a.Status == "Active"))
            .OrderByDescending(us => us.UpdatedAt)
            .Select(us => new SkillVerificationQueueItemResponse(
                us.Id,
                us.UserId,
                us.User.FullName,
                us.User.Email,
                us.User.AvatarUrl,
                us.SkillId,
                us.Skill.Name,
                us.Skill.Category,
                us.Level,
                us.EvidenceUrl,
                us.EvidenceType,
                us.VerificationStatus,
                us.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    // GET /api/counselor/roadmap-approval-queue
    // Hàng đợi duyệt khung roadmap
    [HttpGet("roadmap-approval-queue")]
    [ProducesResponseType<IReadOnlyList<RoadmapApprovalQueueItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoadmapApprovalQueueItemResponse>>> GetRoadmapApprovalQueue(
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();

        var requests = await dbContext.RoadmapApprovalRequests
            .AsNoTracking()
            .Include(r => r.Student)
            .Where(r => r.CounselorId == counselorId && r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RoadmapApprovalQueueItemResponse(
                r.Id,
                r.StudentId,
                r.Student.FullName,
                r.Student.Email,
                r.Student.AvatarUrl,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(requests);
    }

    // GET /api/counselor/roadmap-approval-requests/{id}
    // Xem preview khung roadmap
    [HttpGet("roadmap-approval-requests/{id:guid}")]
    [ProducesResponseType<RoadmapApprovalRequestDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoadmapApprovalRequestDetailsResponse>> GetRoadmapApprovalRequestDetails(
        Guid id,
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();

        var request = await dbContext.RoadmapApprovalRequests
            .Include(r => r.Student)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu duyệt lộ trình." });
        }

        if (request.CounselorId != counselorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Yêu cầu duyệt lộ trình này không thuộc quản lý của bạn." });
        }

        AiRoadmapDto? payload = null;
        try
        {
            payload = JsonSerializer.Deserialize<AiRoadmapDto>(request.PayloadJson);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi khi đọc dữ liệu payload của yêu cầu.", detail = ex.Message });
        }

        if (payload is null)
        {
            return BadRequest(new { message = "Payload của yêu cầu không hợp lệ." });
        }

        return Ok(new RoadmapApprovalRequestDetailsResponse(
            request.Id,
            request.StudentId,
            request.Student.FullName,
            request.Student.Email,
            request.Student.AvatarUrl,
            request.Status,
            payload,
            request.RejectionReason,
            request.CreatedAt));
    }

    // POST /api/counselor/roadmap-approval-requests/{id}/approve
    // Duyệt đề xuất roadmap AI
    [HttpPost("roadmap-approval-requests/{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveRoadmapRequest(
        Guid id,
        [FromServices] IRoadmapMaterializer roadmapMaterializer,
        [FromServices] INotificationService notificationService,
        [FromServices] IAuditLogService auditLog,
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();

        var request = await dbContext.RoadmapApprovalRequests
            .Include(r => r.Student)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu duyệt lộ trình." });
        }

        if (request.CounselorId != counselorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Yêu cầu duyệt lộ trình này không thuộc quản lý của bạn." });
        }

        if (request.Status != "Pending")
        {
            return BadRequest(new { message = "Yêu cầu này không ở trạng thái chờ duyệt." });
        }

        AiRoadmapDto? payload = null;
        try
        {
            payload = JsonSerializer.Deserialize<AiRoadmapDto>(request.PayloadJson);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi khi đọc dữ liệu payload của yêu cầu.", detail = ex.Message });
        }

        if (payload is null)
        {
            return BadRequest(new { message = "Payload của yêu cầu không hợp lệ." });
        }

        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await roadmapMaterializer.MaterializeRoadmapAsync(request.StudentId, payload, cancellationToken);

            request.Status = "Approved";
            request.MaterializedRoadmapId = result.RoadmapId;
            request.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var counselor = await dbContext.Users.FindAsync(new object[] { counselorId }, cancellationToken);
            var counselorName = counselor?.FullName ?? "Cố vấn học tập";

            // Send notification to the student
            await notificationService.SendNotificationAsync(
                userId: request.StudentId,
                type: "RoadmapApprovalApproved",
                title: "Đề xuất lộ trình đã được duyệt",
                message: $"Cố vấn {counselorName} đã duyệt đề xuất lộ trình của bạn: {result.Title}",
                linkUrl: "#roadmap",
                cancellationToken: cancellationToken);

            await auditLog.LogAsync(
                actorUserId: counselorId,
                actorRole: UserRoles.AcademicCounselor,
                action: "RoadmapApprovalApproved",
                entityType: "RoadmapApprovalRequest",
                entityId: request.Id,
                targetUserId: request.StudentId,
                summary: $"Duyệt đề xuất lộ trình và khởi tạo roadmap: {result.Title}",
                metadata: new { roadmapId = result.RoadmapId, result.Title },
                cancellationToken: cancellationToken);

            return Ok(new { message = "Lộ trình đã được phê duyệt và khởi tạo thành công.", roadmapId = result.RoadmapId });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi khi phê duyệt và khởi tạo lộ trình.", detail = ex.Message });
        }
    }

    // POST /api/counselor/roadmap-approval-requests/{id}/reject
    // Từ chối đề xuất roadmap AI
    [HttpPost("roadmap-approval-requests/{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectRoadmapRequest(
        Guid id,
        RejectRoadmapApprovalRequest requestBody,
        [FromServices] INotificationService notificationService,
        [FromServices] IAuditLogService auditLog,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestBody.RejectionReason))
        {
            return BadRequest(new { message = "Lý do từ chối là bắt buộc." });
        }

        var counselorId = GetCurrentUserId();

        var request = await dbContext.RoadmapApprovalRequests
            .Include(r => r.Student)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu duyệt lộ trình." });
        }

        if (request.CounselorId != counselorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Yêu cầu duyệt lộ trình này không thuộc quản lý của bạn." });
        }

        if (request.Status != "Pending")
        {
            return BadRequest(new { message = "Yêu cầu này không ở trạng thái chờ duyệt." });
        }

        request.Status = "Rejected";
        request.RejectionReason = requestBody.RejectionReason.Trim();
        request.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var counselor = await dbContext.Users.FindAsync(new object[] { counselorId }, cancellationToken);
        var counselorName = counselor?.FullName ?? "Cố vấn học tập";

        // Send notification to the student
        await notificationService.SendNotificationAsync(
            userId: request.StudentId,
            type: "RoadmapApprovalRejected",
            title: "Đề xuất lộ trình bị từ chối",
            message: $"Đề xuất lộ trình của bạn đã bị từ chối bởi cố vấn {counselorName}. Lý do: {request.RejectionReason}",
            linkUrl: "#roadmap-requests",
            cancellationToken: cancellationToken);

        await auditLog.LogAsync(
            actorUserId: counselorId,
            actorRole: UserRoles.AcademicCounselor,
            action: "RoadmapApprovalRejected",
            entityType: "RoadmapApprovalRequest",
            entityId: request.Id,
            targetUserId: request.StudentId,
            summary: $"Từ chối đề xuất lộ trình. Lý do: {request.RejectionReason}",
            metadata: new { request.RejectionReason },
            cancellationToken: cancellationToken);

        return Ok(new { message = "Đã từ chối đề xuất lộ trình." });
    }
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
    string? VerifiedLevel,
    string VerificationStatus,
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
    string? CurrentLevel,
    string RequiredLevel,
    string Status,
    int Priority,
    string? Recommendation);

public sealed record SkillVerificationQueueItemResponse(
    Guid UserSkillId,
    Guid StudentId,
    string StudentFullName,
    string StudentEmail,
    string? StudentAvatarUrl,
    Guid SkillId,
    string SkillName,
    string SkillCategory,
    string Level,
    string? EvidenceUrl,
    string? EvidenceType,
    string VerificationStatus,
    DateTimeOffset SubmittedAt);

public sealed record RoadmapApprovalQueueItemResponse(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    string StudentEmail,
    string? StudentAvatarUrl,
    DateTimeOffset CreatedAt);

public sealed record RoadmapApprovalRequestDetailsResponse(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    string StudentEmail,
    string? StudentAvatarUrl,
    string Status,
    AiRoadmapDto Payload,
    string? RejectionReason,
    DateTimeOffset CreatedAt);

public sealed record RejectRoadmapApprovalRequest(
    string RejectionReason);

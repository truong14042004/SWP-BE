using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Contracts.UserSkills;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize]
[Route("api/user-skills")]
public sealed class UserSkillsController(AppDbContext dbContext) : ControllerBase
{
    private static readonly string[] AllowedLevels =
    [
        "Beginner",
        "Intermediate",
        "Advanced",
        "Verified"
    ];

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserSkillResponse>>(StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserSkillResponse>>> GetUserSkills( //tra ve danh sach ky nang cua user
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userSkills = await dbContext.UserSkills
            .AsNoTracking()
            .Include(item => item.Skill) //nap luon bang skill de lay ten va loai ky nang
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.Skill.Category)
            .ThenBy(item => item.Skill.Name)
            .ToListAsync(cancellationToken);

        return Ok(userSkills.Select(ToResponse).ToList());
    }

    [HttpPost]
    [ProducesResponseType<UserSkillResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserSkillResponse>> CreateUserSkill(
        CreateUserSkillRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCreateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var userId = GetCurrentUserId();
        var skill = await dbContext.Skills
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == request.SkillId && item.IsActive, //kiem tra xem skill co ton tai va active khong
                cancellationToken);
        if (skill is null)
        {
            return BadRequest(new { message = "Không tìm thấy kỹ năng hoạt động." });
        }

        var duplicate = await dbContext.UserSkills.AnyAsync(
            item => item.UserId == userId && item.SkillId == request.SkillId,
            cancellationToken);
        if (duplicate)
        {
            return Conflict(new { message = "Kỹ năng này đã được thêm cho người dùng hiện tại." });
        }

        var now = DateTimeOffset.UtcNow;
        var userSkill = new UserSkill
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SkillId = request.SkillId,
            Level = NormalizeLevel(request.Level), //chuan hoa chu hoa/thuong
            EvidenceUrl = request.EvidenceUrl?.Trim(),
            EvidenceType = request.EvidenceType?.Trim(),
            VerificationStatus = !string.IsNullOrWhiteSpace(request.EvidenceUrl)
                ? UserSkillVerificationStatus.PendingVerification
                : UserSkillVerificationStatus.SelfDeclared,
            IsVerified = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.UserSkills.Add(userSkill);
        await dbContext.SaveChangesAsync(cancellationToken);

        userSkill.Skill = skill;

        return CreatedAtAction(nameof(GetUserSkills), ToResponse(userSkill));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<UserSkillResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSkillResponse>> UpdateUserSkill(
        Guid id,
        UpdateUserSkillRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Level is not null)
        {
            var levelError = ValidateLevel(request.Level);
            if (levelError is not null)
            {
                return BadRequest(new { message = levelError }); 
            }
        }

        var evidenceError = ValidateEvidenceFields(request.EvidenceUrl, request.EvidenceType);
        if (evidenceError is not null)
        {
            return BadRequest(new { message = evidenceError });
        }

        var userId = GetCurrentUserId();
        var userSkill = await dbContext.UserSkills
            .Include(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);

        if (userSkill is null)
        {
            return NotFound(new { message = "Không tìm thấy kỹ năng của người dùng." });
        }

        if (request.Level is not null)
        {
            userSkill.Level = NormalizeLevel(request.Level);
        }

        if (request.EvidenceUrl is not null)
        {
            userSkill.EvidenceUrl = string.IsNullOrWhiteSpace(request.EvidenceUrl) 
                ? null //trong thi gan null (tuc la xoa URL bang chung)
                : request.EvidenceUrl.Trim();
        }

        if (request.EvidenceType is not null)
        {
            userSkill.EvidenceType = string.IsNullOrWhiteSpace(request.EvidenceType)
                ? null
                : request.EvidenceType.Trim();
        }

        if (!userSkill.IsVerified)
        {
            if (!string.IsNullOrWhiteSpace(userSkill.EvidenceUrl))
            {
                userSkill.VerificationStatus = UserSkillVerificationStatus.PendingVerification;
                userSkill.RejectionReason = null;
            }
            else
            {
                userSkill.VerificationStatus = UserSkillVerificationStatus.SelfDeclared;
            }
        }

        userSkill.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(userSkill));
    }

    [HttpPost("{id:guid}/submit-evidence")]
    [ProducesResponseType<UserSkillResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserSkillResponse>> SubmitUserSkillEvidence(
        Guid id,
        SubmitUserSkillEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        var evidenceError = ValidateSubmitEvidenceRequest(request.EvidenceUrl, request.EvidenceType);
        if (evidenceError is not null)
        {
            return BadRequest(new { message = evidenceError });
        }

        var userId = GetCurrentUserId();
        var userSkill = await dbContext.UserSkills
            .Include(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);

        if (userSkill is null)
        {
            return NotFound(new { message = "Không tìm thấy kỹ năng của người dùng." });
        }

        if (userSkill.IsVerified)
        {
            return Conflict(new { message = "Kỹ năng đã được xác minh, không thể nộp minh chứng mới." });
        }

        var now = DateTimeOffset.UtcNow;
        userSkill.EvidenceUrl = request.EvidenceUrl.Trim();
        userSkill.EvidenceType = request.EvidenceType.Trim();
        userSkill.VerificationStatus = UserSkillVerificationStatus.PendingVerification;
        userSkill.RejectionReason = null;
        userSkill.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(userSkill));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserSkill(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userSkill = await dbContext.UserSkills
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);

        if (userSkill is null)
        {
            return NotFound(new { message = "Không tìm thấy kỹ năng của người dùng." });
        }

        try
        {
            dbContext.UserSkills.Remove(userSkill);
            await dbContext.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Không thể xóa kỹ năng này vì nó đang được tham chiếu bởi các bản ghi khác." });
        }
    }

    [HttpPost("{id:guid}/verify")]
    // Chỉ Counselor xác minh skill độc lập. Mentor verify skill GIÁN TIẾP qua việc
    // chấm Passed một node roadmap (RoadmapReviewController.ApproveRequest), không
    // qua endpoint này.
    [Authorize(Roles = UserRoles.AcademicCounselor)]
    [ProducesResponseType<UserSkillResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSkillResponse>> VerifyUserSkill(
        Guid id,
        VerifyUserSkillRequest request,
        [FromServices] IAuditLogService auditLog,
        [FromServices] INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        var levelError = ValidateLevel(request.VerifiedLevel);
        if (levelError is not null)
        {
            return BadRequest(new { message = levelError });
        }

        var verifierId = GetCurrentUserId();
        var userSkill = await dbContext.UserSkills
            .Include(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (userSkill is null)
        {
            return NotFound(new { message = "Không tìm thấy kỹ năng của người dùng." });
        }

        if (User.IsInRole(UserRoles.AcademicCounselor)
            && !await IsStudentAssignedToCounselorAsync(userSkill.UserId, verifierId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sinh viên không được phân công cho cố vấn này." });
        }

        var now = DateTimeOffset.UtcNow;
        userSkill.IsVerified = true;
        userSkill.VerifiedLevel = NormalizeLevel(request.VerifiedLevel);
        userSkill.VerifiedByUserId = verifierId;
        userSkill.VerifiedAt = now;
        userSkill.VerificationStatus = UserSkillVerificationStatus.Verified;
        userSkill.RejectionReason = null;
        userSkill.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.LogAsync(
            actorUserId: verifierId,
            actorRole: GetCurrentUserRole(),
            action: "SkillVerified",
            entityType: "UserSkill",
            entityId: userSkill.Id,
            targetUserId: userSkill.UserId,
            summary: $"Xác nhận kỹ năng {userSkill.Skill.Name} ở mức {userSkill.VerifiedLevel}.",
            metadata: new { userSkill.SkillId, userSkill.VerifiedLevel },
            cancellationToken: cancellationToken);

        // Nếu kỹ năng này đang nằm trong một lộ trình Active của sinh viên (node
        // chưa Verified), gợi ý sinh viên tạo lại lộ trình để cắt bớt phần đã đạt.
        var isInActiveRoadmap = await dbContext.RoadmapNodes
            .AnyAsync(node => node.SkillId == userSkill.SkillId
                && node.Roadmap.UserId == userSkill.UserId
                && node.Roadmap.Status == "Active"
                && node.Status != "Verified", cancellationToken);

        if (isInActiveRoadmap)
        {
            await notificationService.SendNotificationAsync(
                userId: userSkill.UserId,
                type: "SkillVerifiedRegenerateRoadmap",
                title: "Kỹ năng đã được xác minh",
                message: $"Kỹ năng {userSkill.Skill.Name} đã được xác minh. Hãy tạo lại lộ trình để cập nhật và bỏ bớt phần bạn đã đạt.",
                linkUrl: "#roadmap",
                cancellationToken: cancellationToken);
        }

        return Ok(ToResponse(userSkill));
    }

    [HttpPost("{id:guid}/unverify")]
    [Authorize(Roles = UserRoles.AcademicCounselor)]
    [ProducesResponseType<UserSkillResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserSkillResponse>> UnverifyUserSkill(
        Guid id,
        [FromServices] INotificationService notificationService,
        [FromServices] IAuditLogService auditLog,
        CancellationToken cancellationToken)
    {
        var counselorId = GetCurrentUserId();
        var userSkill = await dbContext.UserSkills
            .Include(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (userSkill is null)
        {
            return NotFound(new { message = "Không tìm thấy kỹ năng của người dùng." });
        }

        if (!await IsStudentAssignedToCounselorAsync(userSkill.UserId, counselorId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sinh viên không được phân công cho cố vấn này." });
        }

        if (!userSkill.IsVerified)
        {
            return Conflict(new { message = "Kỹ năng hiện chưa được xác minh." });
        }

        var now = DateTimeOffset.UtcNow;
        userSkill.IsVerified = false;
        userSkill.VerifiedByUserId = null;
        userSkill.VerifiedLevel = null;
        userSkill.VerifiedAt = null;
        userSkill.VerificationStatus = UserSkillVerificationStatus.Unverified;
        userSkill.RejectionReason = null;
        userSkill.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        var counselorName = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == counselorId)
            .Select(user => user.FullName)
            .SingleOrDefaultAsync(cancellationToken) ?? "Cố vấn học tập";

        await notificationService.SendNotificationAsync(
            userId: userSkill.UserId,
            type: "SkillVerificationRevoked",
            title: "Xác minh kỹ năng đã bị thu hồi",
            message: $"Cố vấn {counselorName} đã thu hồi xác minh kỹ năng {userSkill.Skill.Name} của bạn.",
            linkUrl: "#skills",
            cancellationToken: cancellationToken);

        await auditLog.LogAsync(
            actorUserId: counselorId,
            actorRole: GetCurrentUserRole(),
            action: "SkillUnverified",
            entityType: "UserSkill",
            entityId: userSkill.Id,
            targetUserId: userSkill.UserId,
            summary: $"Thu hồi xác minh kỹ năng {userSkill.Skill.Name}.",
            metadata: new { userSkill.SkillId },
            cancellationToken: cancellationToken);

        return Ok(ToResponse(userSkill));
    }

    [HttpPost("{id:guid}/reject-evidence")]
    [Authorize(Roles = UserRoles.AcademicCounselor)]
    [ProducesResponseType<UserSkillResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserSkillResponse>> RejectUserSkillEvidence(
        Guid id,
        RejectUserSkillEvidenceRequest request,
        [FromServices] INotificationService notificationService,
        [FromServices] IAuditLogService auditLog,
        CancellationToken cancellationToken)
    {
        var reviewerId = GetCurrentUserId();
        var userSkill = await dbContext.UserSkills
            .Include(item => item.Skill)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (userSkill is null)
        {
            return NotFound(new { message = "Không tìm thấy kỹ năng của người dùng." });
        }

        if (User.IsInRole(UserRoles.AcademicCounselor)
            && !await IsStudentAssignedToCounselorAsync(userSkill.UserId, reviewerId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sinh viên không được phân công cho cố vấn này." });
        }

        if (userSkill.VerificationStatus != UserSkillVerificationStatus.PendingVerification)
        {
            return Conflict(new { message = "Chỉ có thể từ chối kỹ năng đang ở trạng thái chờ xác thực." });
        }

        var now = DateTimeOffset.UtcNow;
        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Minh chứng chưa đủ điều kiện xác thực."
            : request.Reason.Trim();

        userSkill.IsVerified = false;
        userSkill.VerifiedByUserId = null;
        userSkill.VerifiedLevel = null;
        userSkill.VerifiedAt = null;
        userSkill.VerificationStatus = UserSkillVerificationStatus.Unverified;
        userSkill.RejectionReason = reason;
        userSkill.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        var reviewerName = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == reviewerId)
            .Select(user => user.FullName)
            .SingleOrDefaultAsync(cancellationToken) ?? "Người duyệt";

        await notificationService.SendNotificationAsync(
            userId: userSkill.UserId,
            type: "SkillVerificationRejected",
            title: "Minh chứng kỹ năng bị từ chối",
            message: $"{reviewerName} đã từ chối minh chứng kỹ năng {userSkill.Skill.Name}. Lý do: {reason}",
            linkUrl: "#skills",
            cancellationToken: cancellationToken);

        await auditLog.LogAsync(
            actorUserId: reviewerId,
            actorRole: GetCurrentUserRole(),
            action: "SkillEvidenceRejected",
            entityType: "UserSkill",
            entityId: userSkill.Id,
            targetUserId: userSkill.UserId,
            summary: $"Từ chối minh chứng kỹ năng {userSkill.Skill.Name}. Lý do: {reason}",
            metadata: new { userSkill.SkillId, reason },
            cancellationToken: cancellationToken);

        return Ok(ToResponse(userSkill));
    }

    private static string? ValidateCreateRequest(CreateUserSkillRequest request)
    {
        if (request.SkillId == Guid.Empty)
        {
            return "Kỹ năng là bắt buộc.";
        }

        var levelError = ValidateLevel(request.Level);
        if (levelError is not null)
        {
            return levelError;
        }

        return ValidateEvidenceFields(request.EvidenceUrl, request.EvidenceType);
    }

    private static string? ValidateEvidenceFields(string? evidenceUrl, string? evidenceType)
    {
        if (evidenceUrl is { Length: > 1024 })
        {
            return "Đường dẫn minh chứng phải có tối đa 1024 ký tự.";
        }

        if (evidenceType is { Length: > 50 })
        {
            return "Loại minh chứng phải có tối đa 50 ký tự.";
        }

        return null;
    }

    private static string? ValidateSubmitEvidenceRequest(string? evidenceUrl, string? evidenceType)
    {
        if (string.IsNullOrWhiteSpace(evidenceUrl))
        {
            return "Đường dẫn minh chứng là bắt buộc.";
        }

        if (string.IsNullOrWhiteSpace(evidenceType))
        {
            return "Loại minh chứng là bắt buộc.";
        }

        return ValidateEvidenceFields(evidenceUrl, evidenceType);
    }

    private static string? ValidateLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return "Cấp độ là bắt buộc.";
        }

        if (!AllowedLevels.Contains(level.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return "Cấp độ phải là một trong các giá trị: Beginner, Intermediate, Advanced, Verified.";
        }

        return null;
    }

    private static string NormalizeLevel(string level) =>
        AllowedLevels.Single(value => value.Equals(level.Trim(), StringComparison.OrdinalIgnoreCase));

    private async Task<bool> IsStudentAssignedToCounselorAsync(
        Guid studentId,
        Guid counselorId,
        CancellationToken cancellationToken) =>
        await dbContext.CounselorAssignments
            .AnyAsync(
                assignment => assignment.CounselorId == counselorId
                    && assignment.StudentId == studentId
                    && assignment.Status == "Active",
                cancellationToken);

    private static UserSkillResponse ToResponse(UserSkill userSkill) =>
        new(
            userSkill.Id,
            userSkill.SkillId,
            userSkill.Skill.Name,
            userSkill.Skill.Category,
            userSkill.Level,
            userSkill.VerifiedLevel,
            userSkill.EvidenceUrl,
            userSkill.EvidenceType,
            userSkill.VerificationStatus,
            userSkill.RejectionReason,
            userSkill.IsVerified,
            userSkill.VerifiedAt,
            userSkill.CreatedAt,
            userSkill.UpdatedAt);

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Mã xác thực người dùng không hợp lệ.");
    }

    private string GetCurrentUserRole() =>
        User.FindFirstValue(ClaimTypes.Role)
        ?? (User.IsInRole(UserRoles.AcademicCounselor) ? UserRoles.AcademicCounselor
            : User.IsInRole(UserRoles.IndustryMentor) ? UserRoles.IndustryMentor
            : User.IsInRole(UserRoles.Admin) ? UserRoles.Admin
            : "Unknown");
}

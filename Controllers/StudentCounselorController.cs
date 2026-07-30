using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Student)]
[Route("api/students/me/counselor")]
public sealed class StudentCounselorController(AppDbContext dbContext) : ControllerBase
{
    // GET /api/students/me/counselor
    // Sinh viên xem cố vấn học tập đang phụ trách mình (được hệ thống tự phân công
    // sau khi kích hoạt gói dịch vụ, hoặc do admin gán thủ công).
    [HttpGet]
    [ProducesResponseType<MyCounselorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyCounselorResponse>> GetMyCounselor(CancellationToken cancellationToken)
    {
        var studentId = GetCurrentUserId();

        var assignment = await dbContext.CounselorAssignments
            .AsNoTracking()
            .Include(item => item.Counselor)
            .Where(item => item.StudentId == studentId
                && item.Status == "Active"
                && item.Counselor.IsActive)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return NotFound(new { message = "Bạn chưa được phân công cố vấn học tập." });
        }

        return Ok(new MyCounselorResponse(
            assignment.CounselorId,
            assignment.Counselor.FullName,
            assignment.Counselor.Email,
            assignment.Counselor.AvatarUrl,
            assignment.Note,
            assignment.CreatedAt));
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Mã xác thực người dùng không hợp lệ.");
    }
}

public sealed record MyCounselorResponse(
    Guid CounselorId,
    string CounselorName,
    string CounselorEmail,
    string? CounselorAvatarUrl,
    string? Note,
    DateTimeOffset AssignedAt);

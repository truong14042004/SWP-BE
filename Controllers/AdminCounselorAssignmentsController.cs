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
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/counselor-assignments")]
public sealed class AdminCounselorAssignmentsController(AppDbContext dbContext) : ControllerBase
{
    // GET /api/admin/counselor-assignments
    // Lấy toàn bộ danh sách phân công cố vấn - sinh viên
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CounselorAssignmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CounselorAssignmentResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var assignments = await dbContext.CounselorAssignments
            .AsNoTracking()
            .Include(a => a.Counselor)
            .Include(a => a.Student)
            .Include(a => a.AssignedByAdmin)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => ToResponse(a))
            .ToListAsync(cancellationToken);

        return Ok(assignments);
    }

    // POST /api/admin/counselor-assignments
    // Phân công cố vấn cho sinh viên
    [HttpPost]
    [ProducesResponseType<CounselorAssignmentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CounselorAssignmentResponse>> Create(
        CreateCounselorAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CounselorId == Guid.Empty)
        {
            return BadRequest(new { message = "Mã cố vấn là bắt buộc." });
        }

        if (request.StudentId == Guid.Empty)
        {
            return BadRequest(new { message = "Mã sinh viên là bắt buộc." });
        }

        // 1. Kiểm tra Counselor tồn tại và đúng role
        var counselor = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == request.CounselorId && u.IsActive, cancellationToken);

        if (counselor is null)
        {
            return NotFound(new { message = "Không tìm thấy cố vấn đang hoạt động." });
        }

        if (counselor.Role != UserRoles.AcademicCounselor)
        {
            return BadRequest(new { message = $"Người dùng với ID {request.CounselorId} không phải là Cố vấn học tập." });
        }

        // 2. Kiểm tra Student tồn tại và đúng role
        var student = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == request.StudentId && u.IsActive, cancellationToken);

        if (student is null)
        {
            return NotFound(new { message = "Không tìm thấy sinh viên đang hoạt động." });
        }

        if (student.Role != UserRoles.Student)
        {
            return BadRequest(new { message = $"Người dùng với ID {request.StudentId} không phải là Sinh viên." });
        }

        // 3. Kiểm tra trùng lặp
        var existingAssignment = await dbContext.CounselorAssignments
            .SingleOrDefaultAsync(a => a.CounselorId == request.CounselorId && a.StudentId == request.StudentId, cancellationToken);

        if (existingAssignment is not null)
        {
            if (existingAssignment.Status == "Active")
            {
                return Conflict(new { message = "Phân công này đã hoạt động." });
            }
            else
            {
                // Kích hoạt lại phân công cũ
                var adminUserId = GetCurrentUserId();
                existingAssignment.Status = "Active";
                existingAssignment.Note = request.Note?.Trim() ?? existingAssignment.Note;
                existingAssignment.AssignedByAdminId = adminUserId;
                existingAssignment.UpdatedAt = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);
                await dbContext.Entry(existingAssignment).Reference(a => a.Counselor).LoadAsync(cancellationToken);
                await dbContext.Entry(existingAssignment).Reference(a => a.Student).LoadAsync(cancellationToken);
                await dbContext.Entry(existingAssignment).Reference(a => a.AssignedByAdmin).LoadAsync(cancellationToken);

                return Ok(ToResponse(existingAssignment));
            }
        }

        // 4. Tạo phân công mới
        var adminId = GetCurrentUserId();
        var now = DateTimeOffset.UtcNow;
        var assignment = new CounselorAssignment
        {
            Id = Guid.NewGuid(),
            CounselorId = request.CounselorId,
            StudentId = request.StudentId,
            AssignedByAdminId = adminId,
            Status = "Active",
            Note = request.Note?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.CounselorAssignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Load navigation properties for response
        await dbContext.Entry(assignment).Reference(a => a.Counselor).LoadAsync(cancellationToken);
        await dbContext.Entry(assignment).Reference(a => a.Student).LoadAsync(cancellationToken);
        await dbContext.Entry(assignment).Reference(a => a.AssignedByAdmin).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), ToResponse(assignment));
    }

    // DELETE /api/admin/counselor-assignments/{id}
    // Hủy phân công cố vấn - sinh viên (Soft delete - Chuyển sang Inactive)
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.CounselorAssignments
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (assignment is null)
        {
            return NotFound(new { message = "Không tìm thấy phân công cố vấn." });
        }

        assignment.Status = "Inactive";
        assignment.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // PUT /api/admin/counselor-assignments/{id}/enable
    // Kích hoạt lại phân công cố vấn - sinh viên đang Inactive
    [HttpPut("{id:guid}/enable")]
    public async Task<ActionResult<CounselorAssignmentResponse>> Enable(Guid id, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.CounselorAssignments
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (assignment is null)
        {
            return NotFound(new { message = "Không tìm thấy phân công cố vấn." });
        }

        if (assignment.Status == "Active")
        {
            return Conflict(new { message = "Phân công này đã đang hoạt động." });
        }

        var counselor = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == assignment.CounselorId, cancellationToken);
        if (counselor is null || !counselor.IsActive)
        {
            return BadRequest(new { message = "Cố vấn đã bị vô hiệu hóa, không thể kích hoạt lại phân công." });
        }

        var student = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == assignment.StudentId, cancellationToken);
        if (student is null || !student.IsActive)
        {
            return BadRequest(new { message = "Sinh viên đã bị vô hiệu hóa, không thể kích hoạt lại phân công." });
        }

        assignment.Status = "Active";
        assignment.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(assignment).Reference(a => a.Counselor).LoadAsync(cancellationToken);
        await dbContext.Entry(assignment).Reference(a => a.Student).LoadAsync(cancellationToken);
        await dbContext.Entry(assignment).Reference(a => a.AssignedByAdmin).LoadAsync(cancellationToken);

        return Ok(ToResponse(assignment));
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

    private static CounselorAssignmentResponse ToResponse(CounselorAssignment a) =>
        new(
            a.Id,
            a.CounselorId,
            a.Counselor.FullName,
            a.Counselor.Email,
            a.StudentId,
            a.Student.FullName,
            a.Student.Email,
            a.AssignedByAdminId,
            a.AssignedByAdmin.FullName,
            a.Status,
            a.Note,
            a.CreatedAt,
            a.UpdatedAt);
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record CreateCounselorAssignmentRequest(
    Guid CounselorId,
    Guid StudentId,
    string? Note);

public sealed record CounselorAssignmentResponse(
    Guid Id,
    Guid CounselorId,
    string CounselorName,
    string CounselorEmail,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    Guid AssignedByAdminId,
    string AssignedByAdminName,
    string Status,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

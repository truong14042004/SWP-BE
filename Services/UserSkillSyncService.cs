using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Services;

public interface IUserSkillSyncService
{
    /// <summary>
    /// Ghi (hoặc nâng cấp) một kỹ năng vào hồ sơ UserSkills của học viên khi một
    /// module roadmap được Verified. KHÔNG gọi SaveChanges — controller chịu trách
    /// nhiệm lưu trong cùng một transaction (cùng scoped AppDbContext).
    /// </summary>
    Task SyncVerifiedSkillAsync(
        Guid userId,
        Guid skillId,
        Guid careerRoleId,
        Guid verifierId,
        CancellationToken cancellationToken);
}

public sealed class UserSkillSyncService(AppDbContext dbContext) : IUserSkillSyncService
{
    public async Task SyncVerifiedSkillAsync(
        Guid userId,
        Guid skillId,
        Guid careerRoleId,
        Guid verifierId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || skillId == Guid.Empty)
        {
            return;
        }

        // Chỉ đồng bộ kỹ năng còn tồn tại và đang hoạt động.
        var skillIsActive = await dbContext.Skills
            .AnyAsync(skill => skill.Id == skillId && skill.IsActive, cancellationToken);
        if (!skillIsActive)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var existing = await dbContext.UserSkills
            .SingleOrDefaultAsync(
                item => item.UserId == userId && item.SkillId == skillId,
                cancellationToken);

        if (existing is not null)
        {
            // Học viên đã tự khai kỹ năng này: giữ nguyên Level, chỉ đánh dấu đã xác minh.
            if (!existing.IsVerified)
            {
                existing.IsVerified = true;
                existing.VerifiedByUserId = verifierId;
                existing.VerifiedAt = now;
                existing.UpdatedAt = now;
            }

            return;
        }

        var level = await ResolveLevelAsync(careerRoleId, skillId, cancellationToken);

        dbContext.UserSkills.Add(new UserSkill
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SkillId = skillId,
            Level = level,
            IsVerified = true,
            VerifiedByUserId = verifierId,
            VerifiedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    private async Task<string> ResolveLevelAsync(
        Guid careerRoleId,
        Guid skillId,
        CancellationToken cancellationToken)
    {
        var requiredLevel = await dbContext.RoleSkillRequirements
            .AsNoTracking()
            .Where(item => item.CareerRoleId == careerRoleId && item.SkillId == skillId)
            .Select(item => item.RequiredLevel)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(requiredLevel))
        {
            return "Intermediate";
        }

        // Cấp độ hợp lệ của UserSkill: Beginner / Intermediate / Advanced.
        // RoleSkillRequirement có thể là "Expert" -> ánh xạ xuống "Advanced".
        var normalized = requiredLevel.Trim();
        return normalized.Equals("Expert", StringComparison.OrdinalIgnoreCase)
            ? "Advanced"
            : normalized;
    }
}

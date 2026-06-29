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
        string? nodeDifficulty,
        CancellationToken cancellationToken);
}

public sealed class UserSkillSyncService(AppDbContext dbContext) : IUserSkillSyncService
{
    public async Task SyncVerifiedSkillAsync(
        Guid userId,
        Guid skillId,
        Guid careerRoleId,
        Guid verifierId,
        string? nodeDifficulty,
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

        // Ưu tiên level thực của node (Difficulty trên LearningResource) làm VerifiedLevel.
        // Nếu node không có difficulty, fallback về RoleSkillRequirement.RequiredLevel.
        var nodeLevel = DifficultyToLevel(nodeDifficulty);
        var resolvedLevel = !string.IsNullOrWhiteSpace(nodeLevel)
            ? nodeLevel
            : await ResolveLevelAsync(careerRoleId, skillId, cancellationToken);

        if (existing is not null)
        {
            // Học viên đã tự khai kỹ năng này: giữ nguyên Level tự khai, nhưng cập nhật
            // VerifiedLevel lên mức của node vừa được verify (nếu cao hơn mức đã verify).
            if (!existing.IsVerified)
            {
                existing.IsVerified = true;
                existing.VerifiedByUserId = verifierId;
                existing.VerifiedAt = now;
                existing.UpdatedAt = now;
            }

            existing.VerificationStatus = UserSkillVerificationStatus.Verified;

            // Chỉ nâng VerifiedLevel lên mức mới nếu cao hơn mức đã verify hiện tại.
            // Nếu VerifiedLevel đang null, dùng resolvedLevel làm baseline (không dùng
            // Level tự khai vì mentor chưa xác nhận mức đó).
            var currentVerifiedRank = LevelRank(existing.VerifiedLevel);
            if (LevelRank(resolvedLevel) > currentVerifiedRank)
            {
                existing.VerifiedLevel = resolvedLevel;
            }
            else if (string.IsNullOrWhiteSpace(existing.VerifiedLevel))
            {
                existing.VerifiedLevel = resolvedLevel;
            }

            existing.UpdatedAt = now;

            return;
        }

        dbContext.UserSkills.Add(new UserSkill
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SkillId = skillId,
            Level = resolvedLevel,
            VerifiedLevel = resolvedLevel,
            IsVerified = true,
            VerificationStatus = UserSkillVerificationStatus.Verified,
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

    private static int LevelRank(string? level) =>
        level?.Trim().ToLowerInvariant() switch
        {
            "verified" => 4,
            "advanced" => 3,
            "intermediate" => 2,
            "beginner" => 1,
            _ => 0
        };

    private static string? DifficultyToLevel(string? difficulty)
    {
        if (string.IsNullOrWhiteSpace(difficulty))
            return null;

        return difficulty.Trim().ToLowerInvariant() switch
        {
            "beginner" or "basic" or "fundamental" or "fundamentals" or "cơ bản" => "Beginner",
            "intermediate" or "trung cấp" => "Intermediate",
            "advanced" or "nâng cao" => "Advanced",
            "expert" => "Advanced",
            _ => null
        };
    }
}

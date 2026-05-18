using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/admin/stats")]
public sealed class AdminStatsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewStatsResponse>> GetOverview(CancellationToken cancellationToken)
    {
        var users = await BuildUserStats(cancellationToken);
        var subscriptions = await BuildSubscriptionStats(cancellationToken);
        var payments = await BuildPaymentStats(cancellationToken);
        var learningResources = await BuildLearningResourceStats(cancellationToken);
        var careerRoles = await BuildCareerRoleStats(cancellationToken);

        return Ok(new AdminOverviewStatsResponse(
            users,
            subscriptions,
            payments,
            new AdminContentStatsResponse(
                learningResources.TotalSkills,
                learningResources.ActiveSkills,
                learningResources.Total,
                learningResources.Active,
                learningResources.FileResources,
                learningResources.LinkResources,
                careerRoles.Total,
                careerRoles.Active,
                careerRoles.PopularRoles)));
    }

    [HttpGet("users")]
    public async Task<ActionResult<AdminUserStatsResponse>> GetUsers(CancellationToken cancellationToken)
    {
        return Ok(await BuildUserStats(cancellationToken));
    }

    [HttpGet("payments")]
    public async Task<ActionResult<AdminPaymentStatsResponse>> GetPayments(CancellationToken cancellationToken)
    {
        return Ok(await BuildPaymentStats(cancellationToken));
    }

    [HttpGet("subscriptions")]
    public async Task<ActionResult<AdminSubscriptionStatsResponse>> GetSubscriptions(CancellationToken cancellationToken)
    {
        return Ok(await BuildSubscriptionStats(cancellationToken));
    }

    [HttpGet("learning-resources")]
    public async Task<ActionResult<AdminLearningResourceStatsResponse>> GetLearningResources(CancellationToken cancellationToken)
    {
        return Ok(await BuildLearningResourceStats(cancellationToken));
    }

    [HttpGet("career-roles")]
    public async Task<ActionResult<AdminCareerRoleStatsResponse>> GetCareerRoles(CancellationToken cancellationToken)
    {
        return Ok(await BuildCareerRoleStats(cancellationToken));
    }

    private async Task<AdminUserStatsResponse> BuildUserStats(CancellationToken cancellationToken)
    {
        var totalUsers = await dbContext.Users.CountAsync(cancellationToken);
        var activeUsers = await dbContext.Users.CountAsync(user => user.IsActive, cancellationToken);
        var inactiveUsers = totalUsers - activeUsers;

        var usersByRole = await dbContext.Users
            .AsNoTracking()
            .GroupBy(user => user.Role)
            .Select(group => new CountByNameResponse(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ToListAsync(cancellationToken);

        var usersByStatus = new List<CountByNameResponse>
        {
            new("Active", activeUsers),
            new("Inactive", inactiveUsers)
        };

        return new AdminUserStatsResponse(totalUsers, activeUsers, inactiveUsers, usersByRole, usersByStatus);
    }

    private async Task<AdminPaymentStatsResponse> BuildPaymentStats(CancellationToken cancellationToken)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var monthStart = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var totalRevenue = await dbContext.PaymentTransactions
            .Where(payment => payment.Status == "Paid")
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;
        var monthlyRevenue = await dbContext.PaymentTransactions
            .Where(payment => payment.Status == "Paid" && payment.PaidAt >= monthStart)
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;

        var paymentsByStatus = await dbContext.PaymentTransactions
            .AsNoTracking()
            .GroupBy(payment => payment.Status)
            .Select(group => new CountByNameResponse(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ToListAsync(cancellationToken);

        return new AdminPaymentStatsResponse(totalRevenue, monthlyRevenue, paymentsByStatus);
    }

    private async Task<AdminSubscriptionStatsResponse> BuildSubscriptionStats(CancellationToken cancellationToken)
    {
        var activeSubscriptions = await dbContext.Subscriptions
            .CountAsync(subscription => subscription.Status == "Active", cancellationToken);
        var pendingSubscriptions = await dbContext.Subscriptions
            .CountAsync(subscription => subscription.Status == "Pending", cancellationToken);
        var cancelledSubscriptions = await dbContext.Subscriptions
            .CountAsync(subscription => subscription.Status == "Cancelled", cancellationToken);

        var subscriptionsByStatus = await dbContext.Subscriptions
            .AsNoTracking()
            .GroupBy(subscription => subscription.Status)
            .Select(group => new CountByNameResponse(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ToListAsync(cancellationToken);

        var plans = await dbContext.SubscriptionPlans
            .AsNoTracking()
            .OrderBy(plan => plan.Price)
            .Select(plan => new AdminPlanStatResponse(
                plan.Id,
                plan.Name,
                plan.Price,
                plan.Currency,
                plan.BillingCycle,
                plan.IsActive,
                dbContext.Subscriptions.Count(subscription => subscription.PlanId == plan.Id && subscription.Status == "Active")))
            .ToListAsync(cancellationToken);

        return new AdminSubscriptionStatsResponse(
            activeSubscriptions,
            pendingSubscriptions,
            cancelledSubscriptions,
            subscriptionsByStatus,
            plans);
    }

    private async Task<AdminLearningResourceStatsResponse> BuildLearningResourceStats(CancellationToken cancellationToken)
    {
        var totalSkills = await dbContext.Skills.CountAsync(cancellationToken);
        var activeSkills = await dbContext.Skills.CountAsync(skill => skill.IsActive, cancellationToken);
        var totalLearningResources = await dbContext.LearningResources.CountAsync(cancellationToken);
        var activeLearningResources = await dbContext.LearningResources
            .CountAsync(resource => resource.IsActive, cancellationToken);
        var fileLearningResources = await dbContext.LearningResources
            .CountAsync(resource => resource.StorageObjectName != null, cancellationToken);
        var linkLearningResources = totalLearningResources - fileLearningResources;

        var resourcesBySkill = await (
                from resource in dbContext.LearningResources.AsNoTracking()
                join skill in dbContext.Skills.AsNoTracking()
                    on resource.SkillId equals skill.Id into skillJoin
                from skill in skillJoin.DefaultIfEmpty()
                group resource by new
                {
                    resource.SkillId,
                    SkillName = skill == null ? "No skill" : skill.Name
                }
                into groupBySkill
                select new LearningResourceSkillStatResponse(
                    groupBySkill.Key.SkillId,
                    groupBySkill.Key.SkillName,
                    groupBySkill.Count(),
                    groupBySkill.Count(resource => resource.IsActive),
                    groupBySkill.Count(resource => resource.StorageObjectName != null),
                    groupBySkill.Count(resource => resource.StorageObjectName == null)))
            .OrderByDescending(item => item.Total)
            .ThenBy(item => item.SkillName)
            .ToListAsync(cancellationToken);

        var resourcesByType = await dbContext.LearningResources
            .AsNoTracking()
            .GroupBy(resource => resource.ResourceType)
            .Select(group => new CountByNameResponse(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ToListAsync(cancellationToken);

        return new AdminLearningResourceStatsResponse(
            totalSkills,
            activeSkills,
            totalLearningResources,
            activeLearningResources,
            fileLearningResources,
            linkLearningResources,
            resourcesBySkill,
            resourcesByType);
    }

    private async Task<AdminCareerRoleStatsResponse> BuildCareerRoleStats(CancellationToken cancellationToken)
    {
        var totalCareerRoles = await dbContext.CareerRoles.CountAsync(cancellationToken);
        var activeCareerRoles = await dbContext.CareerRoles.CountAsync(role => role.IsActive, cancellationToken);

        var selectedRoleCounts = await dbContext.StudentProfiles
            .AsNoTracking()
            .Where(profile => profile.TargetRoleId != null)
            .GroupBy(profile => profile.TargetRoleId!.Value)
            .Select(group => new
            {
                RoleId = group.Key,
                SelectedCount = group.Count()
            })
            .OrderByDescending(item => item.SelectedCount)
            .Take(10)
            .ToListAsync(cancellationToken);

        var roleIds = selectedRoleCounts.Select(item => item.RoleId).ToList();
        var roleNames = await dbContext.CareerRoles
            .AsNoTracking()
            .Where(role => roleIds.Contains(role.Id))
            .Select(role => new { role.Id, role.Name })
            .ToDictionaryAsync(role => role.Id, role => role.Name, cancellationToken);

        var popularCareerRoles = selectedRoleCounts
            .Select(item => new PopularCareerRoleResponse(
                item.RoleId,
                roleNames.GetValueOrDefault(item.RoleId, "Unknown role"),
                item.SelectedCount))
            .ToList();

        return new AdminCareerRoleStatsResponse(totalCareerRoles, activeCareerRoles, popularCareerRoles);
    }
}

public sealed record AdminOverviewStatsResponse(
    AdminUserStatsResponse Users,
    AdminSubscriptionStatsResponse Subscriptions,
    AdminPaymentStatsResponse Payments,
    AdminContentStatsResponse Content);

public sealed record AdminUserStatsResponse(
    int Total,
    int Active,
    int Inactive,
    IReadOnlyList<CountByNameResponse> ByRole,
    IReadOnlyList<CountByNameResponse> ByStatus);

public sealed record AdminSubscriptionStatsResponse(
    int Active,
    int Pending,
    int Cancelled,
    IReadOnlyList<CountByNameResponse> ByStatus,
    IReadOnlyList<AdminPlanStatResponse> Plans);

public sealed record AdminPaymentStatsResponse(
    decimal TotalRevenue,
    decimal MonthlyRevenue,
    IReadOnlyList<CountByNameResponse> ByStatus);

public sealed record AdminContentStatsResponse(
    int TotalSkills,
    int ActiveSkills,
    int TotalLearningResources,
    int ActiveLearningResources,
    int FileLearningResources,
    int LinkLearningResources,
    int TotalCareerRoles,
    int ActiveCareerRoles,
    IReadOnlyList<PopularCareerRoleResponse> PopularCareerRoles);

public sealed record AdminPlanStatResponse(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    string BillingCycle,
    bool IsActive,
    int ActiveSubscriptions);

public sealed record CountByNameResponse(string Name, int Count);

public sealed record AdminLearningResourceStatsResponse(
    int TotalSkills,
    int ActiveSkills,
    int Total,
    int Active,
    int FileResources,
    int LinkResources,
    IReadOnlyList<LearningResourceSkillStatResponse> BySkill,
    IReadOnlyList<CountByNameResponse> ByResourceType);

public sealed record LearningResourceSkillStatResponse(
    Guid? SkillId,
    string SkillName,
    int Total,
    int Active,
    int FileResources,
    int LinkResources);

public sealed record AdminCareerRoleStatsResponse(
    int Total,
    int Active,
    IReadOnlyList<PopularCareerRoleResponse> PopularRoles);

public sealed record PopularCareerRoleResponse(
    Guid Id,
    string Name,
    int SelectedCount);

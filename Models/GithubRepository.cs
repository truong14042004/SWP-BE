using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class GithubRepository
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string RepoName { get; set; } = null!;

    public string RepoUrl { get; set; } = null!;

    public string? Description { get; set; }

    public string? MainLanguage { get; set; }

    public string? ReadmeContent { get; set; }

    public string? AiSummary { get; set; }

    public string? TechStackJson { get; set; }

    public decimal? QualityScore { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<GithubRepositorySkill> GithubRepositorySkills { get; set; } = new List<GithubRepositorySkill>();

    public virtual ICollection<MentorFeedback> MentorFeedbacks { get; set; } = new List<MentorFeedback>();

    public virtual ICollection<PortfolioProject> PortfolioProjects { get; set; } = new List<PortfolioProject>();

    public virtual User User { get; set; } = null!;
}

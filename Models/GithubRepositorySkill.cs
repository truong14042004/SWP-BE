using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class GithubRepositorySkill
{
    public Guid Id { get; set; }

    public Guid GithubRepositoryId { get; set; }

    public Guid SkillId { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public string? EvidenceText { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual GithubRepository GithubRepository { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}

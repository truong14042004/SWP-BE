using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class SkillGapReportItem
{
    public Guid Id { get; set; }

    public Guid SkillGapReportId { get; set; }

    public Guid SkillId { get; set; }

    public string? CurrentLevel { get; set; }

    public string RequiredLevel { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int Priority { get; set; }

    public string? Recommendation { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Skill Skill { get; set; } = null!;

    public virtual SkillGapReport SkillGapReport { get; set; } = null!;
}

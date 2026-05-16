using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class CounselorFeedback
{
    public Guid Id { get; set; }

    public Guid CounselorId { get; set; }

    public Guid StudentId { get; set; }

    public Guid? RoadmapId { get; set; }

    public Guid? SkillGapReportId { get; set; }

    public string Comment { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User Counselor { get; set; } = null!;

    public virtual Roadmap? Roadmap { get; set; }

    public virtual SkillGapReport? SkillGapReport { get; set; }

    public virtual User Student { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class Roadmap
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CareerRoleId { get; set; }

    public Guid? SkillGapReportId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public decimal Progress { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CareerRole CareerRole { get; set; } = null!;

    public virtual ICollection<CounselorFeedback> CounselorFeedbacks { get; set; } = new List<CounselorFeedback>();

    public virtual ICollection<RoadmapNode> RoadmapNodes { get; set; } = new List<RoadmapNode>();

    public virtual SkillGapReport? SkillGapReport { get; set; }

    public virtual User User { get; set; } = null!;
}

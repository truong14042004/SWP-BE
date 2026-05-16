using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class SkillGapReport
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CareerRoleId { get; set; }

    public decimal MatchScore { get; set; }

    public string? Summary { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CareerRole CareerRole { get; set; } = null!;

    public virtual ICollection<CounselorFeedback> CounselorFeedbacks { get; set; } = new List<CounselorFeedback>();

    public virtual ICollection<Roadmap> Roadmaps { get; set; } = new List<Roadmap>();

    public virtual ICollection<SkillGapReportItem> SkillGapReportItems { get; set; } = new List<SkillGapReportItem>();

    public virtual User User { get; set; } = null!;
}

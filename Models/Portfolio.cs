using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class Portfolio
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Slug { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Bio { get; set; }

    public string? Theme { get; set; }

    public bool IsPublished { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<MentorFeedback> MentorFeedbacks { get; set; } = new List<MentorFeedback>();

    public virtual ICollection<PortfolioProject> PortfolioProjects { get; set; } = new List<PortfolioProject>();

    public virtual User User { get; set; } = null!;
}

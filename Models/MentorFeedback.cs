using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class MentorFeedback
{
    public Guid Id { get; set; }

    public Guid MentorId { get; set; }

    public Guid StudentId { get; set; }

    public Guid? PortfolioId { get; set; }

    public Guid? GithubRepositoryId { get; set; }

    public string Comment { get; set; } = null!;

    public int? Rating { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual GithubRepository? GithubRepository { get; set; }

    public virtual User Mentor { get; set; } = null!;

    public virtual Portfolio? Portfolio { get; set; }

    public virtual User Student { get; set; } = null!;
}

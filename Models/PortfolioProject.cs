using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class PortfolioProject
{
    public Guid Id { get; set; }

    public Guid PortfolioId { get; set; }

    public Guid? GithubRepositoryId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? TechStackJson { get; set; }

    public string? DemoUrl { get; set; }

    public string? SourceUrl { get; set; }

    public int OrderIndex { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual GithubRepository? GithubRepository { get; set; }

    public virtual Portfolio Portfolio { get; set; } = null!;
}

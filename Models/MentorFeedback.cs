namespace SWP_BE.Models;

public sealed class MentorFeedback
{
    public Guid Id { get; set; }
    public Guid MentorId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? PortfolioId { get; set; }
    public Guid? GithubRepositoryId { get; set; }

    /// <summary>
    /// Tom tat danh gia tong (free text).
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// Diem tong the 1-5.
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// Nhan xet ve chat luong portfolio (cau truc, trinh bay, story telling).
    /// </summary>
    public string? PortfolioQualityFeedback { get; set; }

    /// <summary>
    /// Danh gia ky nang ky thuat dua tren repo va project.
    /// </summary>
    public string? TechnicalSkillsAssessment { get; set; }

    /// <summary>
    /// Nhan xet ve chat luong project: scope, do hoan thien, dem do phuc tap.
    /// </summary>
    public string? ProjectQualityFeedback { get; set; }

    /// <summary>
    /// Khuyen nghi cu the cho sinh vien.
    /// </summary>
    public string? Recommendations { get; set; }

    /// <summary>
    /// Muc do san sang cho cong viec: NotReady | NeedsImprovement | Ready | Excellent.
    /// </summary>
    public string? JobReadinessLevel { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User Mentor { get; set; } = null!;
    public User Student { get; set; } = null!;
    public Portfolio? Portfolio { get; set; }
    public GithubRepository? GithubRepository { get; set; }
}

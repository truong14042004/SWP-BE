namespace SWP_BE.Models;

public sealed class MentorProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? Company { get; set; }

    public string? JobTitle { get; set; }

    public string? Bio { get; set; }

    public int? YearsOfExperience { get; set; }

    public string? LinkedInUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}

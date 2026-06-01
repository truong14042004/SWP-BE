using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWP_BE.Models;

[Table("student_talent_profiles")]
public class StudentTalentProfile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid StudentId { get; set; }

    [Required]
    [MaxLength(500)]
    public string AnalyzedRepoUrl { get; set; } = string.Empty;

    // Điểm hệ số: 1 đến 10
    public int LogicalThinkingScore { get; set; } = 5;
    
    public int SystemArchitectureScore { get; set; } = 5;
    
    public int VisualDesignScore { get; set; } = 5;

    [Column(TypeName = "text")]
    public string AiFeedback { get; set; } = string.Empty;

    public DateTimeOffset AnalyzedAt { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey("StudentId")]
    public User Student { get; set; } = null!;
}

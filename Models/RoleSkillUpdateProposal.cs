namespace SWP_BE.Models;

public sealed class RoleSkillUpdateProposal
{
    public Guid Id { get; set; }
    public Guid CareerRoleId { get; set; }
    public Guid SkillId { get; set; }
    
    // Tên kỹ năng được lưu lại để hiển thị, đề phòng kỹ năng bị đổi tên
    public string SkillName { get; set; } = string.Empty;
    
    // "UpdatePriority", "UpdateWeight", hoặc "AddSkill"
    public string ActionType { get; set; } = string.Empty; 
    
    public int? CurrentPriority { get; set; }
    public int? ProposedPriority { get; set; }
    
    public decimal? CurrentWeight { get; set; }
    public decimal? ProposedWeight { get; set; }
    
    public string Reason { get; set; } = string.Empty;
    
    // "Pending", "Approved", "Rejected"
    public string Status { get; set; } = "Pending";
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }

    public CareerRole CareerRole { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}

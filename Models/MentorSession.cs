using System;
using System.Collections.Generic;

namespace SWP_BE.Models;

public partial class MentorSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Question { get; set; } = null!;

    public string Answer { get; set; } = null!;

    public string? ContextJson { get; set; }

    public string? Model { get; set; }

    public int? TokensUsed { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}

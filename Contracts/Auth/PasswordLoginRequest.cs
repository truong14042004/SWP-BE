using System.ComponentModel.DataAnnotations;

namespace SWP_BE.Contracts.Auth;

public sealed record PasswordLoginRequest(
    [Required]
    [StringLength(100, MinimumLength = 3)]
    string Username,

    [Required]
    [StringLength(100, MinimumLength = 6)]
    string Password);

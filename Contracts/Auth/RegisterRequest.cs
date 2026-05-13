using System.ComponentModel.DataAnnotations;

namespace SWP_BE.Contracts.Auth;

public sealed record RegisterRequest(
    [Required]
    [StringLength(100, MinimumLength = 3)]
    string Username,

    [Required]
    [EmailAddress]
    [StringLength(256)]
    string Email,

    [Required]
    [StringLength(200, MinimumLength = 1)]
    string FullName,

    [Required]
    [StringLength(100, MinimumLength = 6)]
    string Password);

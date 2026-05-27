using System.ComponentModel.DataAnnotations;

namespace SWP_BE.Contracts.Auth;

public sealed record RegisterRequest(
    [Required]
    [StringLength(32, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9._-]+$")]
    string Username,

    [Required]
    [EmailAddress]
    [StringLength(256)]
    [RegularExpression("^[a-zA-Z0-9](?:[a-zA-Z0-9._%+-]{0,62}[a-zA-Z0-9])?@gmail\\.com$")]
    string Email,

    [Required]
    [StringLength(200, MinimumLength = 1)]
    string FullName,

    [Required]
    [StringLength(100, MinimumLength = 8)]
    string Password,

    [Required]
    [StringLength(100, MinimumLength = 8)]
    string ConfirmPassword);

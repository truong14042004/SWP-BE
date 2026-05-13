using System.ComponentModel.DataAnnotations;

namespace SWP_BE.Contracts.Auth;

public sealed record VerifyEmailOtpRequest(
    [Required]
    [EmailAddress]
    [StringLength(256)]
    string Email,

    [Required]
    [StringLength(6, MinimumLength = 6)]
    string Otp);

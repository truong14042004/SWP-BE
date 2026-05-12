using System.ComponentModel.DataAnnotations;

namespace SWP_BE.Contracts.Auth;

public sealed record GoogleLoginRequest(
    [Required] string IdToken);

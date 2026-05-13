namespace SWP_BE.Contracts.Auth;

public sealed record AuthMessageResponse(
    string Message,
    string Email);

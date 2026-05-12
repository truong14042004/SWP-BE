using SWP_BE.Models;

namespace SWP_BE.Services;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user);
}

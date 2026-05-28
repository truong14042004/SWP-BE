using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using SWP_BE.Contracts.Auth;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Services;

public sealed class PasswordAuthService(
    AppDbContext dbContext,
    IRefreshTokenService refreshTokenService,
    IPasswordHasher<User> passwordHasher,
    IEmailSender emailSender) : IPasswordAuthService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
    private static readonly Regex UsernameRegex = new("^[a-z0-9._-]{3,32}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex GmailRegex = new("^[a-z0-9](?:[a-z0-9._%+-]{0,62}[a-z0-9])?@gmail\\.com$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<AuthMessageResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var username = Normalize(request.Username);
        var email = Normalize(request.Email);
        var fullName = request.FullName.Trim();

        ValidateRegistration(username, email, fullName, request.Password);

        if (request.Password != request.ConfirmPassword)
        {
            throw new InvalidOperationException("Mật khẩu xác nhận không trùng khớp.");
        }

        var now = DateTimeOffset.UtcNow;
        await dbContext.PendingRegistrations
            .Where(registration => registration.EmailVerificationOtpExpiresAt < now)
            .ExecuteDeleteAsync(cancellationToken);

        var activeUserExists = await dbContext.Users
            .AnyAsync(user => (user.Username == username || user.Email == email) && user.IsActive, cancellationToken);
        if (activeUserExists)
        {
            throw new InvalidOperationException("Tên đăng nhập hoặc email đã tồn tại.");
        }

        var otp = CreateOtp();
        var hashUser = new User { Username = username, Email = email, FullName = fullName };
        var pendingRegistration = await dbContext.PendingRegistrations
            .SingleOrDefaultAsync(
                registration => registration.Email == email || registration.Username == username,
                cancellationToken);

        if (pendingRegistration is not null
            && (pendingRegistration.Email != email || pendingRegistration.Username != username))
        {
            throw new InvalidOperationException("Tên đăng nhập hoặc email đã tồn tại.");
        }

        if (pendingRegistration is null)
        {
            pendingRegistration = new PendingRegistration
            {
                Id = Guid.NewGuid(),
                CreatedAt = now
            };
            dbContext.PendingRegistrations.Add(pendingRegistration);
        }

        pendingRegistration.Username = username;
        pendingRegistration.Email = email;
        pendingRegistration.FullName = fullName;
        pendingRegistration.PasswordHash = passwordHasher.HashPassword(hashUser, request.Password);
        pendingRegistration.EmailVerificationOtpHash = passwordHasher.HashPassword(hashUser, otp);
        pendingRegistration.EmailVerificationOtpExpiresAt = now.Add(OtpLifetime);
        pendingRegistration.UpdatedAt = now;

        await emailSender.SendOtpAsync(pendingRegistration.Email, pendingRegistration.FullName, otp, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthMessageResponse(
            "Mã OTP xác thực đã được gửi đến email của bạn.",
            pendingRegistration.Email);
    }

    public async Task<AuthMessageResponse> ResendOtpAsync(
        ResendOtpRequest request,
        CancellationToken cancellationToken)
    {
        var email = Normalize(request.Email);

        var verifiedActiveUserExists = await dbContext.Users
            .AnyAsync(user => user.Email == email && user.IsEmailVerified && user.IsActive, cancellationToken);
        if (verifiedActiveUserExists)
        {
            throw new InvalidOperationException("Email đã được đăng ký và xác thực.");
        }

        var pendingRegistration = await dbContext.PendingRegistrations
            .SingleOrDefaultAsync(registration => registration.Email == email, cancellationToken);
        if (pendingRegistration is null)
        {
            throw new InvalidOperationException("Không tìm thấy thông tin đăng ký. Vui lòng đăng ký trước.");
        }

        var otp = CreateOtp();
        var hashUser = new User
        {
            Username = pendingRegistration.Username,
            Email = pendingRegistration.Email,
            FullName = pendingRegistration.FullName
        };
        var now = DateTimeOffset.UtcNow;

        pendingRegistration.EmailVerificationOtpHash = passwordHasher.HashPassword(hashUser, otp);
        pendingRegistration.EmailVerificationOtpExpiresAt = now.Add(TimeSpan.FromMinutes(15));
        pendingRegistration.UpdatedAt = now;

        await emailSender.SendOtpAsync(pendingRegistration.Email, pendingRegistration.FullName, otp, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthMessageResponse(
            "Mã OTP xác thực đã được gửi lại đến email của bạn.",
            email);
    }

    public async Task<AuthResponse> VerifyEmailOtpAsync(
        VerifyEmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        var email = Normalize(request.Email);
        var pendingRegistration = await dbContext.PendingRegistrations
            .SingleOrDefaultAsync(registration => registration.Email == email, cancellationToken);

        if (pendingRegistration is null
            || pendingRegistration.EmailVerificationOtpExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("Mã OTP không hợp lệ hoặc đã hết hạn.");
        }

        var hashUser = new User
        {
            Username = pendingRegistration.Username,
            Email = pendingRegistration.Email,
            FullName = pendingRegistration.FullName
        };
        var result = passwordHasher.VerifyHashedPassword(
            hashUser,
            pendingRegistration.EmailVerificationOtpHash,
            request.Otp);

        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Mã OTP không hợp lệ hoặc đã hết hạn.");
        }

        var activeUserExists = await dbContext.Users.AnyAsync(
            user => (user.Username == pendingRegistration.Username || user.Email == pendingRegistration.Email) && user.IsActive,
            cancellationToken);
        if (activeUserExists)
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc email đã tồn tại.");
        }

        // Remove conflicting inactive users to avoid unique key constraint violations
        var conflictingInactiveUsers = await dbContext.Users
            .Where(user => (user.Username == pendingRegistration.Username || user.Email == pendingRegistration.Email) && !user.IsActive)
            .ToListAsync(cancellationToken);
        if (conflictingInactiveUsers.Count > 0)
        {
            dbContext.Users.RemoveRange(conflictingInactiveUsers);
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = pendingRegistration.Username,
            Email = pendingRegistration.Email,
            FullName = pendingRegistration.FullName,
            PasswordHash = pendingRegistration.PasswordHash,
            Role = UserRoles.Student,
            IsActive = true,
            IsEmailVerified = true,
            EmailVerifiedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Users.Add(user);
        dbContext.PendingRegistrations.Remove(pendingRegistration);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await refreshTokenService.CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(
        PasswordLoginRequest request,
        CancellationToken cancellationToken)
    {
        var username = Normalize(request.Username);
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(existingUser => existingUser.Username == username, cancellationToken);

        if (user is null
            || string.IsNullOrWhiteSpace(user.PasswordHash)
            || !user.IsActive
            || !user.IsEmailVerified)
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await refreshTokenService.CreateSessionAsync(user, cancellationToken);
    }

    private static string CreateOtp() =>
        RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    private static void ValidateRegistration(string username, string email, string fullName, string password)
    {
        if (fullName.Length < 2)
        {
            throw new InvalidOperationException("Họ và tên phải có ít nhất 2 ký tự.");
        }

        if (!UsernameRegex.IsMatch(username))
        {
            throw new InvalidOperationException("Tên đăng nhập phải dài 3-32 ký tự và chỉ gồm chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.");
        }

        if (!GmailRegex.IsMatch(email))
        {
            throw new InvalidOperationException("Email phải là địa chỉ Gmail hợp lệ, ví dụ name@gmail.com.");
        }

        if (password.Length < 8)
        {
            throw new InvalidOperationException("Mật khẩu phải có ít nhất 8 ký tự.");
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            throw new InvalidOperationException("Mật khẩu phải có ít nhất 1 chữ cái và 1 chữ số.");
        }
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}

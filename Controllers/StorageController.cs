using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Options;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Route("api/storage")]
public sealed partial class StorageController(
    AppDbContext dbContext,
    IFileStorageService storageService,
    IOptions<StorageOptions> storageOptions) : ControllerBase
{
    private static readonly HashSet<string> ImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private static readonly HashSet<string> EvidenceContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf",
        "application/zip",
        "application/x-zip-compressed",
        "application/x-compressed"
    };

    private static readonly HashSet<string> GeneralContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "application/pdf",
        "application/zip",
        "application/x-zip-compressed",
        "application/x-compressed",
        "text/plain",
        "text/markdown"
    };

    private readonly StorageOptions options = storageOptions.Value;

    [Authorize]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StorageFileResponse>> Upload(
        [FromForm] UploadFileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var category = NormalizeCategory(request.Category);
        var allowedTypes = GetAllowedContentTypes(category);
        ValidateFile(request.File, allowedTypes);

        var objectName = BuildUserObjectName(userId, category, request.File.FileName, request.File.ContentType);
        await using var stream = request.File.OpenReadStream();
        var result = await storageService.UploadAsync(stream, objectName, request.File.ContentType, cancellationToken);

        return Ok(ToResponse(result));
    }

    [Authorize]
    [HttpPost("import-url")]
    public async Task<ActionResult<StorageFileResponse>> ImportUrl(
        ImportUrlRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var sourceUrl = ValidateSourceUrl(request.Url);
        var category = NormalizeCategory(request.Category);
        var allowedTypes = GetAllowedContentTypes(category);
        var objectName = BuildUserObjectName(userId, category, request.FileName ?? sourceUrl.Segments.LastOrDefault(), null);
        var result = await storageService.ImportFromUrlAsync(
            sourceUrl,
            objectName,
            allowedTypes,
            options.MaxUploadBytes,
            cancellationToken);

        return Ok(ToResponse(result));
    }

    [Authorize]
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StorageFileResponse>> UploadAvatar(
        [FromForm] UploadSingleFileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        ValidateFile(request.File, ImageContentTypes);

        var user = await dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "Mã xác thực người dùng không hợp lệ." });
        }

        var objectName = BuildUserObjectName(userId, "avatar", request.File.FileName, request.File.ContentType);
        await using var stream = request.File.OpenReadStream();
        var result = await storageService.UploadAsync(stream, objectName, request.File.ContentType, cancellationToken);

        user.AvatarUrl = result.ObjectName;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(result));
    }

    [Authorize]
    [HttpPost("avatar/import-url")]
    public async Task<ActionResult<StorageFileResponse>> ImportAvatarUrl(
        ImportSingleUrlRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var sourceUrl = ValidateSourceUrl(request.Url);
        var user = await dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "Mã xác thực người dùng không hợp lệ." });
        }

        var objectName = BuildUserObjectName(userId, "avatar", request.FileName ?? sourceUrl.Segments.LastOrDefault(), null);
        var result = await storageService.ImportFromUrlAsync(
            sourceUrl,
            objectName,
            ImageContentTypes,
            options.MaxUploadBytes,
            cancellationToken);

        user.AvatarUrl = result.ObjectName;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(result));
    }

    [Authorize]
    [HttpPost("user-skills/{userSkillId:guid}/evidence")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StorageFileResponse>> UploadSkillEvidence(
        Guid userSkillId,
        [FromForm] UploadSingleFileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        ValidateFile(request.File, EvidenceContentTypes);

        var userSkill = await dbContext.UserSkills
            .FirstOrDefaultAsync(item => item.Id == userSkillId && item.UserId == userId, cancellationToken);
        if (userSkill is null)
        {
            return NotFound(new { message = "Không tìm thấy kỹ năng của người dùng." });
        }

        if (userSkill.IsVerified)
        {
            return Conflict(new { message = "Kỹ năng đã được xác minh, không thể nộp minh chứng mới." });
        }

        var objectName = BuildUserObjectName(userId, "evidence", request.File.FileName, request.File.ContentType);
        await using var stream = request.File.OpenReadStream();
        var result = await storageService.UploadAsync(stream, objectName, request.File.ContentType, cancellationToken);

        userSkill.EvidenceUrl = result.ObjectName;
        userSkill.EvidenceType = result.ContentType;
        // Có minh chứng mới thì kỹ năng phải vào hàng đợi xác thực — cùng luật với
        // UserSkillsController (create/update/submit-evidence); nếu bỏ sót, kỹ năng
        // kẹt ở SelfDeclared và không bao giờ xuất hiện trong queue của cố vấn.
        userSkill.VerificationStatus = UserSkillVerificationStatus.PendingVerification;
        userSkill.RejectionReason = null;
        userSkill.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(result));
    }

    [Authorize]
    [HttpPost("user-skills/{userSkillId:guid}/evidence/import-url")]
    public async Task<ActionResult<StorageFileResponse>> ImportSkillEvidenceUrl(
        Guid userSkillId,
        ImportSingleUrlRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var sourceUrl = ValidateSourceUrl(request.Url);
        var userSkill = await dbContext.UserSkills
            .FirstOrDefaultAsync(item => item.Id == userSkillId && item.UserId == userId, cancellationToken);
        if (userSkill is null)
        {
            return NotFound(new { message = "Không tìm thấy kỹ năng của người dùng." });
        }

        if (userSkill.IsVerified)
        {
            return Conflict(new { message = "Kỹ năng đã được xác minh, không thể nộp minh chứng mới." });
        }

        var objectName = BuildUserObjectName(userId, "evidence", request.FileName ?? sourceUrl.Segments.LastOrDefault(), null);
        var result = await storageService.ImportFromUrlAsync(
            sourceUrl,
            objectName,
            EvidenceContentTypes,
            options.MaxUploadBytes,
            cancellationToken);

        userSkill.EvidenceUrl = result.ObjectName;
        userSkill.EvidenceType = result.ContentType;
        // Cùng luật auto-transition PendingVerification như luồng upload file ở trên.
        userSkill.VerificationStatus = UserSkillVerificationStatus.PendingVerification;
        userSkill.RejectionReason = null;
        userSkill.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(result));
    }

    [Authorize]
    [HttpPost("portfolio-projects/{projectId:guid}/image")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<StorageFileResponse>> UploadPortfolioProjectImage(
        Guid projectId,
        [FromForm] UploadSingleFileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        ValidateFile(request.File, ImageContentTypes);

        var project = await GetOwnedPortfolioProject(projectId, userId, cancellationToken);
        if (project is null)
        {
            return NotFound(new { message = "Không tìm thấy dự án trong hồ sơ cá nhân." });
        }

        var objectName = BuildUserObjectName(userId, "portfolio", request.File.FileName, request.File.ContentType);
        await using var stream = request.File.OpenReadStream();
        var result = await storageService.UploadAsync(stream, objectName, request.File.ContentType, cancellationToken);

        project.ImageUrl = result.ObjectName;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(result));
    }

    [Authorize]
    [HttpPost("portfolio-projects/{projectId:guid}/image/import-url")]
    public async Task<ActionResult<StorageFileResponse>> ImportPortfolioProjectImageUrl(
        Guid projectId,
        ImportSingleUrlRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var sourceUrl = ValidateSourceUrl(request.Url);
        var project = await GetOwnedPortfolioProject(projectId, userId, cancellationToken);
        if (project is null)
        {
            return NotFound(new { message = "Không tìm thấy dự án trong hồ sơ cá nhân." });
        }

        var objectName = BuildUserObjectName(userId, "portfolio", request.FileName ?? sourceUrl.Segments.LastOrDefault(), null);
        var result = await storageService.ImportFromUrlAsync(
            sourceUrl,
            objectName,
            ImageContentTypes,
            options.MaxUploadBytes,
            cancellationToken);

        project.ImageUrl = result.ObjectName;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(result));
    }

    [Authorize]
    [HttpGet("download")]
    public async Task<IActionResult> Download(
        [FromQuery] string objectName,
        CancellationToken cancellationToken)
    {
        var normalizedObjectName = NormalizeObjectName(objectName);
        var userId = GetCurrentUserId();
        if (!await CanReadUserObjectAsync(userId, normalizedObjectName, cancellationToken))
        {
            return Forbid();
        }

        return await DownloadObject(normalizedObjectName, cancellationToken);
    }

    [Authorize]
    [HttpGet("signed-url")]
    public async Task<ActionResult<SignedUrlResponse>> CreateSignedUrl(
        [FromQuery] string objectName,
        CancellationToken cancellationToken)
    {
        var normalizedObjectName = NormalizeObjectName(objectName);
        var userId = GetCurrentUserId();
        if (!await CanReadUserObjectAsync(userId, normalizedObjectName, cancellationToken))
        {
            return Forbid();
        }

        var duration = TimeSpan.FromMinutes(Math.Clamp(options.SignedUrlMinutes, 1, 60));
        var url = await storageService.CreateSignedReadUrlAsync(normalizedObjectName, duration, cancellationToken);
        return Ok(new SignedUrlResponse(normalizedObjectName, url, DateTimeOffset.UtcNow.Add(duration)));
    }

    [Authorize]
    [HttpGet("learning-resources/{resourceId:guid}/download")]
    public async Task<IActionResult> DownloadLearningResource(
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var resource = await GetReadableLearningResource(resourceId, cancellationToken);
        if (resource is null || !LearningResourceFiles.IsStoredFile(resource.StorageObjectName))
        {
            return NotFound(new { message = "Không tìm thấy tệp tài nguyên học tập." });
        }

        return await DownloadObject(resource.StorageObjectName!, cancellationToken);
    }

    [Authorize]
    [HttpGet("learning-resources/{resourceId:guid}/signed-url")]
    public async Task<ActionResult<SignedUrlResponse>> CreateLearningResourceSignedUrl(
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var resource = await GetReadableLearningResource(resourceId, cancellationToken);
        if (resource is null || !LearningResourceFiles.IsStoredFile(resource.StorageObjectName))
        {
            return NotFound(new { message = "Không tìm thấy tệp tài nguyên học tập." });
        }

        var duration = TimeSpan.FromMinutes(Math.Clamp(options.SignedUrlMinutes, 1, 60));
        var url = await storageService.CreateSignedReadUrlAsync(resource.StorageObjectName!, duration, cancellationToken);
        return Ok(new SignedUrlResponse(resource.StorageObjectName!, url, DateTimeOffset.UtcNow.Add(duration)));
    }

    [HttpGet("public/portfolio-projects/{projectId:guid}/image")]
    public async Task<ActionResult<SignedUrlResponse>> CreatePublicPortfolioImageSignedUrl(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await GetPublishedPortfolioProject(projectId, cancellationToken);
        if (project?.ImageUrl is null)
        {
            return NotFound(new { message = "Không tìm thấy ảnh của dự án hồ sơ cá nhân." });
        }

        var duration = TimeSpan.FromMinutes(Math.Clamp(options.SignedUrlMinutes, 1, 60));
        var url = await storageService.CreateSignedReadUrlAsync(project.ImageUrl, duration, cancellationToken);
        return Ok(new SignedUrlResponse(project.ImageUrl, url, DateTimeOffset.UtcNow.Add(duration)));
    }

    [HttpGet("public/portfolio-projects/{projectId:guid}/image/download")]
    public async Task<IActionResult> DownloadPublicPortfolioImage(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await GetPublishedPortfolioProject(projectId, cancellationToken);
        if (project?.ImageUrl is null)
        {
            return NotFound(new { message = "Không tìm thấy ảnh của dự án hồ sơ cá nhân." });
        }

        return await DownloadObject(project.ImageUrl, cancellationToken);
    }

    [HttpGet("public/users/{userId:guid}/avatar/download")]
    public async Task<IActionResult> DownloadPublicUserAvatar(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user?.AvatarUrl is null)
        {
            return NotFound(new { message = "Không tìm thấy ảnh đại diện của người dùng." });
        }

        if (Uri.TryCreate(user.AvatarUrl, UriKind.Absolute, out var avatarUri))
        {
            return Redirect(avatarUri.ToString());
        }

        if (!IsUserObject(userId, user.AvatarUrl))
        {
            return NotFound(new { message = "Không tìm thấy ảnh đại diện của người dùng." });
        }

        return await DownloadObject(user.AvatarUrl, cancellationToken);
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery] string objectName,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!IsUserObject(userId, objectName))
        {
            return Forbid();
        }

        await storageService.DeleteAsync(objectName, cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult> DownloadObject(string objectName, CancellationToken cancellationToken)
    {
        var result = await storageService.DownloadAsync(objectName, cancellationToken);
        var fileName = Path.GetFileName(objectName);
        return File(result.Content, result.ContentType, fileName, enableRangeProcessing: true);
    }

    private async Task<PortfolioProject?> GetOwnedPortfolioProject(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PortfolioProjects
            .Include(project => project.Portfolio)
            .FirstOrDefaultAsync(
                project => project.Id == projectId && project.Portfolio.UserId == userId,
                cancellationToken);
    }

    private async Task<PortfolioProject?> GetPublishedPortfolioProject(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PortfolioProjects
            .Include(project => project.Portfolio)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                project => project.Id == projectId && project.Portfolio.IsPublished,
                cancellationToken);
    }

    private async Task<LearningResource?> GetReadableLearningResource(
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.LearningResources.AsNoTracking().Where(resource => resource.Id == resourceId);
        if (!User.IsInRole(UserRoles.Admin))
        {
            query = query.Where(resource => resource.IsActive);
        }

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    private StorageFileResponse ToResponse(StoredFileResult result)
    {
        return new StorageFileResponse(
            result.ObjectName,
            result.ContentType,
            result.Size,
            $"/api/storage/download?objectName={Uri.EscapeDataString(result.ObjectName)}",
            result.UploadedAt);
    }

    private void ValidateFile(IFormFile? file, IReadOnlySet<string> allowedContentTypes)
    {
        if (file is null || file.Length == 0)
        {
            throw new InvalidOperationException("Tệp tin là bắt buộc.");
        }

        if (file.Length > options.MaxUploadBytes)
        {
            throw new InvalidOperationException($"Tệp tin quá lớn. Kích thước tối đa là {options.MaxUploadBytes} bytes.");
        }

        if (!allowedContentTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException($"Định dạng tệp không được hỗ trợ: {file.ContentType}.");
        }
    }

    private static IReadOnlySet<string> GetAllowedContentTypes(string category)
    {
        return category switch
        {
            "avatar" or "portfolio" => ImageContentTypes,
            "evidence" or "reports" or "invoices" => EvidenceContentTypes,
            _ => GeneralContentTypes
        };
    }

    private static string NormalizeCategory(string? category)
    {
        var normalized = string.IsNullOrWhiteSpace(category)
            ? "general"
            : SafeSegmentRegex().Replace(category.Trim().ToLowerInvariant(), "-").Trim('-');

        return string.IsNullOrWhiteSpace(normalized)
            ? "general"
            : normalized;
    }

    private static string BuildUserObjectName(
        Guid userId,
        string category,
        string? originalFileName,
        string? contentType)
    {
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = GetExtension(contentType);
        }

        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        baseName = string.IsNullOrWhiteSpace(baseName)
            ? "file"
            : SafeSegmentRegex().Replace(baseName.Trim().ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "file";
        }

        extension = SafeExtensionRegex().IsMatch(extension)
            ? extension.ToLowerInvariant()
            : string.Empty;

        return $"users/{userId}/{category}/{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}-{baseName}{extension}";
    }

    private static string GetExtension(string? contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "application/pdf" => ".pdf",
            "application/zip" => ".zip",
            "application/x-zip-compressed" => ".zip",
            "application/x-compressed" => ".zip",
            "text/plain" => ".txt",
            "text/markdown" => ".md",
            _ => string.Empty
        };
    }

    private static bool IsUserObject(Guid userId, string objectName)
    {
        return !string.IsNullOrWhiteSpace(objectName)
            && !objectName.Contains("..", StringComparison.Ordinal)
            && objectName.StartsWith($"users/{userId}/", StringComparison.Ordinal);
    }

    // Reviewer chỉ được đọc file của sinh viên có quan hệ thực với mình (assignment
    // Active hoặc review request), không phải mọi object trong bucket theo role —
    // nếu không, bất kỳ mentor/counselor nào biết objectName đều tải được CV/minh
    // chứng của sinh viên không liên quan.
    private async Task<bool> CanReadUserObjectAsync(
        Guid currentUserId,
        string objectName,
        CancellationToken cancellationToken)
    {
        if (IsUserObject(currentUserId, objectName))
        {
            return true;
        }

        if (User.IsInRole(UserRoles.Admin))
        {
            return true;
        }

        var ownerId = TryGetObjectOwnerId(objectName);
        if (ownerId is null)
        {
            return false;
        }

        if (User.IsInRole(UserRoles.AcademicCounselor)
            && await dbContext.CounselorAssignments.AnyAsync(
                assignment => assignment.CounselorId == currentUserId
                    && assignment.StudentId == ownerId
                    && assignment.Status == "Active",
                cancellationToken))
        {
            return true;
        }

        if (User.IsInRole(UserRoles.AcademicCounselor) || User.IsInRole(UserRoles.IndustryMentor))
        {
            if (await dbContext.RoadmapNodeReviewRequests.AnyAsync(
                request => request.ReviewerId == currentUserId && request.StudentId == ownerId,
                cancellationToken))
            {
                return true;
            }
        }

        // Mentor review portfolio: IndustryMentorController cho mentor xem hồ sơ/CV/kỹ năng
        // của mọi sinh viên đã publish portfolio (review-queue) — quyền đọc file phải khớp,
        // nếu không mentor thấy metadata nhưng bấm xem CV/minh chứng là 403.
        if (User.IsInRole(UserRoles.IndustryMentor))
        {
            return await dbContext.Portfolios.AnyAsync(
                portfolio => portfolio.UserId == ownerId && portfolio.IsPublished,
                cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Chuẩn hoá objectName: dữ liệu cũ có thể lưu nguyên download path
    /// ("/api/storage/download?objectName=users%2F...") thay vì object name thô —
    /// bóc query ra để authorization lẫn ký URL hoạt động với cả hai dạng.
    /// </summary>
    private static string NormalizeObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName) || !objectName.Contains("objectName=", StringComparison.OrdinalIgnoreCase))
        {
            return objectName;
        }

        var queryIndex = objectName.IndexOf('?', StringComparison.Ordinal);
        var query = queryIndex >= 0 ? objectName[(queryIndex + 1)..] : objectName;
        foreach (var pair in query.Split('&'))
        {
            var separator = pair.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0
                && pair[..separator].Equals("objectName", StringComparison.OrdinalIgnoreCase))
            {
                var value = Uri.UnescapeDataString(pair[(separator + 1)..]);
                return string.IsNullOrWhiteSpace(value) ? objectName : value;
            }
        }

        return objectName;
    }

    private static Guid? TryGetObjectOwnerId(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName)
            || objectName.Contains("..", StringComparison.Ordinal)
            || !objectName.StartsWith("users/", StringComparison.Ordinal))
        {
            return null;
        }

        var segments = objectName.Split('/');
        return segments.Length > 1 && Guid.TryParse(segments[1], out var ownerId)
            ? ownerId
            : null;
    }

    private static Uri ValidateSourceUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Yêu cầu một đường dẫn URL hợp lệ.");
        }

        return uri;
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Mã xác thực người dùng không hợp lệ.");
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SafeSegmentRegex();

    [GeneratedRegex("^\\.[a-z0-9]{1,10}$")]
    private static partial Regex SafeExtensionRegex();
}

public sealed class UploadFileRequest
{
    public IFormFile File { get; set; } = null!;
    public string? Category { get; set; }
}

public sealed class UploadSingleFileRequest
{
    public IFormFile File { get; set; } = null!;
}

public sealed record ImportUrlRequest(string Url, string? Category, string? FileName);

public sealed record ImportSingleUrlRequest(string Url, string? FileName);

public sealed record StorageFileResponse(
    string ObjectName,
    string ContentType,
    long Size,
    string DownloadPath,
    DateTimeOffset UploadedAt);

public sealed record SignedUrlResponse(
    string ObjectName,
    string Url,
    DateTimeOffset ExpiresAt);

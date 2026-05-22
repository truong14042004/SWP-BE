using Microsoft.AspNetCore.Http;

namespace SWP_BE.Contracts.Profile;

public sealed record UploadCvRequest(IFormFile File);

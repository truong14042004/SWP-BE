namespace SWP_BE.Services;

/// <summary>
/// Nhận diện tài nguyên học tập có file thật trên GCS hay chỉ là link ngoài.
/// Dữ liệu legacy có StorageObjectName = "auto:youtube"/"auto:docs" (marker của
/// tài nguyên auto-sinh cũ) — KHÔNG phải object GCS; nếu trả SourceType = "File"
/// thì FE sẽ gọi download/signed-url và lỗi.
/// </summary>
public static class LearningResourceFiles
{
    private const string LegacyAutoResourceMarker = "auto:";

    public static bool IsStoredFile(string? storageObjectName) =>
        !string.IsNullOrWhiteSpace(storageObjectName)
        && !storageObjectName.StartsWith(LegacyAutoResourceMarker, StringComparison.OrdinalIgnoreCase);

    public static string SourceType(string? storageObjectName) =>
        IsStoredFile(storageObjectName) ? "File" : "Link";

    /// <summary>
    /// Tên file gốc từ StorageObjectName dạng
    /// "learning-resources/{id}/{timestamp17}-{guid32}-{tên-gốc}{ext}"; null nếu không phải file.
    /// </summary>
    public static string? GetFileName(string? storageObjectName)
    {
        if (!IsStoredFile(storageObjectName))
        {
            return null;
        }

        var fileName = Path.GetFileName(storageObjectName!.Replace("\\", "/"));
        var parts = fileName.Split('-', 3, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 3 && parts[0].Length == 17 && parts[1].Length == 32
            ? parts[2]
            : fileName;
    }
}

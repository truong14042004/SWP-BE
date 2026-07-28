using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Services;

public sealed class RoadmapResourceProvisioner(AppDbContext dbContext) : IRoadmapResourceProvisioner
{
    // Nhãn cũ trong StorageObjectName của tài nguyên auto-sinh (dữ liệu legacy).
    // Không dùng cho bản ghi mới nữa: StorageObjectName != null làm DTO trả
    // SourceType = "File" và FE gọi download GCS với object không tồn tại.
    private const string LegacyAutoResourceMarker = "auto:";

    // Tài nguyên auto-sinh nhận diện bằng chính URL curated (chỉ provisioner sinh dạng này).
    private const string AutoYoutubePrefix = "https://www.youtube.com/results?search_query=";
    private const string AutoDocsPrefix = "https://www.google.com/search?q=";

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> EnsureMinimumResourcesAsync(
        IReadOnlyList<NodeResourceContext> nodes,
        int minResources,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, IReadOnlyList<Guid>>();
        if (nodes.Count == 0)
        {
            return result;
        }

        // Cache các LearningResource auto đã tồn tại (idempotent theo Url) để không tạo trùng
        // khi nhiều node cùng một chủ đề hoặc khi regenerate roadmap. Dùng GroupBy thay cho
        // ToDictionary để an toàn nếu lỡ tồn tại 2 bản ghi auto cùng Url (vd 2 request đồng thời).
        var existingAuto = (await dbContext.LearningResources
            .Where(resource => resource.IsActive
                && ((resource.StorageObjectName != null
                        && resource.StorageObjectName.StartsWith(LegacyAutoResourceMarker))
                    || resource.Url.StartsWith(AutoYoutubePrefix)
                    || resource.Url.StartsWith(AutoDocsPrefix)))
            .Select(resource => new { resource.Url, resource.Id })
            .ToListAsync(cancellationToken))
            .GroupBy(resource => resource.Url, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);

        // Resource vừa tạo trong vòng lặp này nhưng chưa SaveChanges.
        var pendingByUrl = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes)
        {
            var missing = minResources - node.ExistingCount;
            if (missing <= 0)
            {
                continue;
            }

            var topic = NormalizeTopic(node.Topic);
            var candidates = BuildCuratedResources(topic, node.TargetDifficulty);
            var assigned = new List<Guid>();

            foreach (var candidate in candidates)
            {
                if (assigned.Count >= missing)
                {
                    break;
                }

                if (existingAuto.TryGetValue(candidate.Url, out var existingId))
                {
                    assigned.Add(existingId);
                    continue;
                }

                if (pendingByUrl.TryGetValue(candidate.Url, out var pendingId))
                {
                    assigned.Add(pendingId);
                    continue;
                }

                var resource = new LearningResource
                {
                    Id = Guid.NewGuid(),
                    // SkillId = null để resource auto KHÔNG lọt lại pool tài nguyên theo skill
                    // ở lần generate/regenerate sau (tránh trùng FK và ô nhiễm dữ liệu curated).
                    SkillId = null,
                    Title = candidate.Title,
                    Url = candidate.Url,
                    // Đây là link ngoài, không phải file tải lên — StorageObjectName phải null
                    // để DTO trả SourceType = "Link" (marker cũ khiến FE gọi download GCS lỗi).
                    StorageObjectName = null,
                    ResourceType = candidate.ResourceType,
                    Difficulty = candidate.Difficulty,
                    EstimatedHours = candidate.EstimatedHours,
                    LessonNumber = 1,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                dbContext.LearningResources.Add(resource);
                pendingByUrl[candidate.Url] = resource.Id;
                assigned.Add(resource.Id);
            }

            if (assigned.Count > 0)
            {
                result[node.NodeId] = assigned;
            }
        }

        return result;
    }

    // Chuẩn hóa chủ đề dùng để sinh link tài nguyên. Bỏ tiền tố động từ tiếng Việt
    // ("Học ", "Cải thiện "...) để giữ lại tên kỹ năng/công nghệ. Đặt tại đây để cả
    // RoadmapController lẫn RoadmapMaterializer dùng chung một logic → link sinh ra
    // nhất quán, idempotent giữa hai luồng.
    private static readonly string[] TopicPrefixes =
        ["Học ", "Cải thiện ", "Thành thạo ", "Luyện tập ", "Xây dựng ", "Củng cố "];

    private static string NormalizeTopic(string? rawTopic)
    {
        if (string.IsNullOrWhiteSpace(rawTopic))
        {
            return "lập trình";
        }

        var topic = rawTopic.Trim();
        foreach (var prefix in TopicPrefixes)
        {
            if (topic.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var stripped = topic[prefix.Length..].Trim();
                if (stripped.Length > 0)
                {
                    topic = stripped;
                }
                break;
            }
        }

        return topic;
    }

    private static IReadOnlyList<CuratedResource> BuildCuratedResources(string topic, string? targetDifficulty)
    {
        // Đưa cấp độ vào từ khóa tìm kiếm: link theo level khác nhau có URL khác nhau,
        // vừa giữ idempotency theo Url vừa cho kết quả đúng trình độ node đang dạy.
        var difficulty = SkillLevels.RankToDifficulty(SkillLevels.DifficultyRank(targetDifficulty));
        var levelKeyword = difficulty?.ToLowerInvariant();
        var searchTopic = levelKeyword is null ? topic : $"{topic} {levelKeyword}";
        var encoded = Uri.EscapeDataString(searchTopic + " tutorial");
        var encodedDocs = Uri.EscapeDataString(searchTopic + " documentation");
        var titleSuffix = difficulty is null ? string.Empty : $" ({difficulty})";

        // Thứ tự ưu tiên: 1 video (YouTube) + 1 documentation, đúng tinh thần FR2.3.
        return
        [
            new CuratedResource(
                $"Video hướng dẫn: {topic}{titleSuffix}",
                $"{AutoYoutubePrefix}{encoded}",
                "Video",
                difficulty ?? "Beginner",
                4),
            new CuratedResource(
                $"Tài liệu tham khảo: {topic}{titleSuffix}",
                $"{AutoDocsPrefix}{encodedDocs}",
                "Documentation",
                difficulty ?? "Beginner",
                4)
        ];
    }

    private sealed record CuratedResource(
        string Title,
        string Url,
        string ResourceType,
        string Difficulty,
        int EstimatedHours);
}

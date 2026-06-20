using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Services;

public sealed class RoadmapResourceProvisioner(AppDbContext dbContext) : IRoadmapResourceProvisioner
{
    // Tài nguyên auto-sinh được gắn nhãn để phân biệt với tài nguyên do admin nhập.
    private const string AutoResourceMarker = "auto:";

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
            .Where(resource => resource.StorageObjectName != null
                && resource.StorageObjectName.StartsWith(AutoResourceMarker))
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
            var candidates = BuildCuratedResources(topic);
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
                    // Đánh dấu nguồn gốc auto-sinh; không phải file tải lên.
                    StorageObjectName = AutoResourceMarker + candidate.Kind,
                    ResourceType = candidate.ResourceType,
                    Difficulty = "Beginner",
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

    private static IReadOnlyList<CuratedResource> BuildCuratedResources(string topic)
    {
        var encoded = Uri.EscapeDataString(topic + " tutorial");
        var encodedDocs = Uri.EscapeDataString(topic + " documentation");

        // Thứ tự ưu tiên: 1 video (YouTube) + 1 documentation, đúng tinh thần FR2.3.
        return
        [
            new CuratedResource(
                $"Video hướng dẫn: {topic}",
                $"https://www.youtube.com/results?search_query={encoded}",
                "Video",
                "youtube",
                4),
            new CuratedResource(
                $"Tài liệu tham khảo: {topic}",
                $"https://www.google.com/search?q={encodedDocs}",
                "Documentation",
                "docs",
                4)
        ];
    }

    private sealed record CuratedResource(
        string Title,
        string Url,
        string ResourceType,
        string Kind,
        int EstimatedHours);
}

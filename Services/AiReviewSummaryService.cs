using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;

namespace SWP_BE.Services;

public sealed class AiReviewSummaryService(
    AppDbContext dbContext,
    IAiTextGenerationService aiTextGeneration,
    IHttpClientFactory httpClientFactory,
    ILogger<AiReviewSummaryService> logger) : IAiReviewSummaryService
{
    // Han che de tranh nhoi prompt qua dai.
    private const int MaxReadmeChars = 12_000;
    private static readonly Regex GithubRepoUrlPattern = new(
        @"^https?://github\.com/(?<owner>[A-Za-z0-9_.-]+)/(?<repo>[A-Za-z0-9_.-]+?)(?:\.git)?/?(?:[?#].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<AiReviewSummary> GenerateAsync(
        Guid reviewRequestId,
        Guid generatedByUserId,
        CancellationToken cancellationToken)
    {
        var request = await dbContext.RoadmapNodeReviewRequests
            .Include(item => item.RoadmapNode)
            .ThenInclude(node => node.Roadmap)
            .ThenInclude(roadmap => roadmap.CareerRole)
            .Include(item => item.RoadmapNode)
            .ThenInclude(node => node.Skill)
            .Include(item => item.Student)
            .Include(item => item.AiSummary)
            .SingleOrDefaultAsync(item => item.Id == reviewRequestId, cancellationToken);

        if (request is null)
        {
            throw new InvalidOperationException("Khong tim thay yeu cau review.");
        }

        if (!string.Equals(request.EvidenceType, "GitRepository", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("AI review chi ho tro evidence la Git Repository.");
        }

        if (string.IsNullOrWhiteSpace(request.EvidenceUrl))
        {
            throw new InvalidOperationException("Yeu cau review khong dinh kem URL repository.");
        }

        var match = GithubRepoUrlPattern.Match(request.EvidenceUrl.Trim());
        if (!match.Success)
        {
            throw new InvalidOperationException("URL evidence khong phai la GitHub repo hop le.");
        }

        var owner = match.Groups["owner"].Value;
        var repo = match.Groups["repo"].Value;

        // Lay GitHub token cua student (neu da OAuth) -> tang rate limit + truy cap repo private cua chinh ho.
        var studentToken = await dbContext.GithubConnections
            .AsNoTracking()
            .Where(connection => connection.UserId == request.StudentId)
            .Select(connection => connection.AccessToken)
            .SingleOrDefaultAsync(cancellationToken);

        var repoFacts = await FetchRepoFactsAsync(owner, repo, studentToken, cancellationToken);

        var talentProfile = await dbContext.Set<StudentTalentProfile>()
            .Where(t => t.StudentId == request.StudentId)
            .OrderByDescending(t => t.AnalyzedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var systemInstruction = BuildSystemInstruction();
        var userPrompt = BuildUserPrompt(request, owner, repo, repoFacts, talentProfile);

        AiTextResult aiResult;
        try
        {
            aiResult = await aiTextGeneration.GenerateAsync(systemInstruction, userPrompt, asJson: true, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gemini that bai khi sinh AiReviewSummary cho request {RequestId}", reviewRequestId);
            throw new InvalidOperationException("AI khong phan hoi. Vui long thu lai sau.", ex);
        }

        var parsed = ParseAiJson(aiResult.Text);

        var now = DateTimeOffset.UtcNow;

        // Xoa summary cu (neu co) - moi lan bam la quet lai tu dau.
        if (request.AiSummary is not null)
        {
            request.AiSummaryId = null;
            dbContext.AiReviewSummaries.Remove(request.AiSummary);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var summary = new AiReviewSummary
        {
            Id = Guid.NewGuid(),
            ReviewRequestId = request.Id,
            GeneratedByUserId = generatedByUserId,
            EvidenceType = request.EvidenceType!,
            EvidenceUrl = request.EvidenceUrl!,
            Model = aiResult.Model,
            TokensUsed = aiResult.TokensUsed,
            TechStackJson = parsed.TechStackJson,
            StrengthsJson = parsed.StrengthsJson,
            WeaknessesJson = parsed.WeaknessesJson,
            SuggestedQuestionsJson = parsed.SuggestedQuestionsJson,
            SkillMappingJson = parsed.SkillMappingJson,
            OverallSummary = parsed.OverallSummary,
            GeneratedAt = now
        };

        dbContext.AiReviewSummaries.Add(summary);
        request.AiSummaryId = summary.Id;

        await dbContext.SaveChangesAsync(cancellationToken);

        return summary;
    }

    private async Task<GithubRepoFacts> FetchRepoFactsAsync(
        string owner,
        string repo,
        string? authToken,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.github.com/");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SWP-BE-CareerMap/1.0");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        if (!string.IsNullOrWhiteSpace(authToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }

        var repoTask = GetJsonAsync(client, $"repos/{owner}/{repo}", cancellationToken);
        var readmeTask = GetJsonAsync(client, $"repos/{owner}/{repo}/readme", cancellationToken);
        var languagesTask = GetJsonAsync(client, $"repos/{owner}/{repo}/languages", cancellationToken);

        await Task.WhenAll(repoTask, readmeTask, languagesTask);

        var repoDoc = repoTask.Result;
        var readmeDoc = readmeTask.Result;
        var languagesDoc = languagesTask.Result;

        string? description = null;
        string? primaryLanguage = null;
        if (repoDoc is not null)
        {
            description = TryGetString(repoDoc.RootElement, "description");
            primaryLanguage = TryGetString(repoDoc.RootElement, "language");
        }

        string? readmeText = null;
        if (readmeDoc is not null
            && readmeDoc.RootElement.TryGetProperty("content", out var contentElement)
            && contentElement.ValueKind == JsonValueKind.String)
        {
            try
            {
                var raw = contentElement.GetString() ?? string.Empty;
                var cleaned = raw.Replace("\n", string.Empty).Replace("\r", string.Empty);
                var bytes = Convert.FromBase64String(cleaned);
                readmeText = Encoding.UTF8.GetString(bytes);
                if (readmeText.Length > MaxReadmeChars)
                {
                    readmeText = readmeText[..MaxReadmeChars] + "\n... (truncated)";
                }
            }
            catch (FormatException)
            {
                logger.LogWarning("README base64 cua {Owner}/{Repo} khong giai ma duoc", owner, repo);
            }
        }

        var languages = new List<string>();
        if (languagesDoc is not null && languagesDoc.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in languagesDoc.RootElement.EnumerateObject())
            {
                languages.Add(property.Name);
            }
        }

        return new GithubRepoFacts(description, primaryLanguage, languages, readmeText);
    }

    private async Task<JsonDocument?> GetJsonAsync(HttpClient client, string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("GitHub API {Path} tra ve {Status}", path, (int)response.StatusCode);
                return null;
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Loi khi goi GitHub API {Path}", path);
            return null;
        }
    }

    private static string BuildSystemInstruction() =>
        """
        Ban la mot mentor IT dang ho tro mot mentor con nguoi review evidence (mot GitHub repository) ma sinh vien
        gui kem yeu cau review cho mot module trong lo trinh hoc. Nhiem vu cua ban la tom tat va danh gia repo do
        de mentor co cai nhin nhanh, KHONG thay the danh gia cua mentor.

        QUY TAC:
        - Chi dua tren noi dung repo va thong tin module duoc cung cap. Khong bia.
        - Neu thong tin khong du, ghi ro "Khong du du lieu" trong cac truong tuong ung.
        - Tra ve JSON DUNG schema duoc yeu cau, khong them text ngoai JSON.
        - Viet bang tieng Viet, ngan gon, danh cho mentor co kinh nghiem (khong giai thich co ban).
        """;

    private static string BuildUserPrompt(
        RoadmapNodeReviewRequest request,
        string owner,
        string repo,
        GithubRepoFacts facts,
        StudentTalentProfile? talentProfile)
    {
        var node = request.RoadmapNode;
        var nodeSkill = node.Skill?.Name ?? "(chua gan skill cu the)";
        var careerRole = node.Roadmap?.CareerRole?.Name ?? "(chua xac dinh)";
        var studentName = request.Student.FullName;
        var studentNote = string.IsNullOrWhiteSpace(request.StudentNote)
            ? "(sinh vien khong ghi chu them)"
            : request.StudentNote.Trim();

        var languagesText = facts.Languages.Count == 0
            ? "(khong xac dinh)"
            : string.Join(", ", facts.Languages);

        var readme = string.IsNullOrWhiteSpace(facts.ReadmeText)
            ? "(khong co README hoac khong doc duoc)"
            : facts.ReadmeText.Trim();

        var talentSection = talentProfile is null
            ? "(Chua co ho so tai nang truoc do)"
            : $"- Tu duy logic: {talentProfile.LogicalThinkingScore}/10\n- Kien truc he thong: {talentProfile.SystemArchitectureScore}/10\n- Thiet ke (Visual): {talentProfile.VisualDesignScore}/10\n- Nhan xet gan nhat: {talentProfile.AiFeedback}";

        return $$"""
        ## BOI CANH NODE LO TRINH
        - Career role muc tieu: {{careerRole}}
        - Title node: {{node.Title}}
        - Mo ta node: {{node.Description ?? "(khong co)"}}
        - Skill chinh cua node: {{nodeSkill}}

        ## SINH VIEN
        - Ten: {{studentName}}
        - Ghi chu kem theo evidence: {{studentNote}}

        ## HO SO TAI NANG (TALENT PROFILE)
        {{talentSection}}

        ## REPOSITORY (GitHub)
        - URL: https://github.com/{{owner}}/{{repo}}
        - Mo ta repo: {{facts.Description ?? "(khong co mo ta)"}}
        - Ngon ngu chinh: {{facts.PrimaryLanguage ?? "(khong xac dinh)"}}
        - Cac ngon ngu phat hien: {{languagesText}}

        ## README (toi da {{MaxReadmeChars}} ky tu)
        ```
        {{readme}}
        ```

        ## NHIEM VU
        Tra ve mot JSON object voi cac field DUNG ten sau:
        {
          "techStack": ["string"],
          "strengths": ["string"],
          "weaknesses": ["string"],
          "suggestedQuestions": ["string"],
          "skillMapping": {
            "matchesNode": true,
            "reason": "string",
            "missingAspects": ["string"]
          },
          "overallSummary": "string"
        }

        Rang buoc:
        - techStack: toi da 10 muc (vd "React 18", "PostgreSQL", "EF Core").
        - strengths / weaknesses: toi da 5 muc moi mang, ngan gon.
        - suggestedQuestions: dung 3 cau hoi mentor co the hoi sinh vien.
        - skillMapping.matchesNode: true neu evidence thuc su demo duoc skill cua node, nguoc lai false.
        - skillMapping.missingAspects: cac khia canh node yeu cau ma evidence chua the hien.
        - overallSummary: 1-2 cau tong ket (hay lien he/danh gia su phu hop voi HO SO TAI NANG cua sinh vien).
        """;
    }

    private static ParsedSummary ParseAiJson(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            return new ParsedSummary(
                TechStackJson: ExtractArrayJson(root, "techStack"),
                StrengthsJson: ExtractArrayJson(root, "strengths"),
                WeaknessesJson: ExtractArrayJson(root, "weaknesses"),
                SuggestedQuestionsJson: ExtractArrayJson(root, "suggestedQuestions"),
                SkillMappingJson: ExtractObjectJson(root, "skillMapping"),
                OverallSummary: TryGetString(root, "overallSummary"));
        }
        catch (JsonException)
        {
            // Fallback: gan raw text vao overall, cac array de rong.
            return new ParsedSummary(
                TechStackJson: "[]",
                StrengthsJson: "[]",
                WeaknessesJson: "[]",
                SuggestedQuestionsJson: "[]",
                SkillMappingJson: "{}",
                OverallSummary: raw.Length > 2000 ? raw[..2000] : raw);
        }
    }

    private static string ExtractArrayJson(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.Array)
        {
            return element.GetRawText();
        }

        return "[]";
    }

    private static string ExtractObjectJson(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.Object)
        {
            return element.GetRawText();
        }

        return "{}";
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    private sealed record GithubRepoFacts(
        string? Description,
        string? PrimaryLanguage,
        List<string> Languages,
        string? ReadmeText);

    private sealed record ParsedSummary(
        string TechStackJson,
        string StrengthsJson,
        string WeaknessesJson,
        string SuggestedQuestionsJson,
        string SkillMappingJson,
        string? OverallSummary);
}

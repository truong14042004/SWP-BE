using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize]
[Route("api/mentor")]
public sealed class MentorController(
    AppDbContext dbContext,
    IAiTextGenerationService aiTextGenerationService,
    ILogger<MentorController> logger) : ControllerBase
{
    private const int FreePlanAiChatLimit = 20;

    private static readonly JsonSerializerOptions LooseJson = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [HttpPost("chat")]
    public async Task<ActionResult<MentorSessionResponse>> Chat(
        MentorChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { message = "Question is required." });
        }

        var userId = GetCurrentUserId();

        // Quota check (Free plan: 20 messages/period)
        var quota = await GetAiChatQuotaAsync(userId, cancellationToken);
        if (quota.Remaining <= 0)
        {
            return StatusCode(StatusCodes.Status402PaymentRequired, new
            {
                message = $"Bạn đã dùng hết {quota.Used}/{quota.Limit} lượt AI Mentor. Hãy nâng cấp gói để tiếp tục.",
                quota
            });
        }

        var context = await BuildMentorContext(userId, request.ContextJson, cancellationToken);

        AiTextResult aiResult;
        try
        {
            aiResult = await aiTextGenerationService.GenerateAsync(
                BuildSystemInstruction(),
                $"""
                Student context:
                {context}

                Student question:
                {request.Question.Trim()}
                """,
                asJson: true,
                cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Gemini call failed for user {UserId}", userId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = exception.Message });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected AI mentor failure for user {UserId}", userId);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "AI Mentor tạm thời không phản hồi. Hãy thử lại sau ít phút.",
                detail = exception.Message
            });
        }

        // Parse AI JSON output. Fallback: if not valid JSON, treat whole text as Q&A answer.
        var parsed = ParseAiResponse(aiResult.Text);

        var now = DateTimeOffset.UtcNow;
        var sessionPayload = new
        {
            answer = parsed.Answer,
            intent = parsed.Intent,
            suggestions = parsed.Suggestions
        };
        var payloadJson = JsonSerializer.Serialize(sessionPayload, LooseJson);

        var session = new MentorSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Question = request.Question.Trim(),
            Answer = payloadJson,
            ContextJson = context,
            Model = aiResult.Model,
            TokensUsed = aiResult.TokensUsed,
            CreatedAt = now
        };

        try
        {
            dbContext.MentorSessions.Add(session);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException dbEx)
        {
            var inner = dbEx.InnerException?.Message ?? dbEx.Message;
            logger.LogError(dbEx, "Failed to save MentorSession for user {UserId}. Inner: {Inner}", userId, inner);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Không lưu được phiên chat AI Mentor.",
                detail = inner,
                type = "DbUpdateException"
            });
        }

        return Ok(ToResponse(session, parsed, quota with { Used = quota.Used + 1, Remaining = Math.Max(quota.Remaining - 1, 0) }));
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<MentorSessionResponse>>> GetSessions(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var sessions = await dbContext.MentorSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.CreatedAt)
            .ToListAsync(cancellationToken);

        var responses = sessions
            .Select(session => ToResponse(session, ParseAiResponse(session.Answer), null))
            .ToList();

        return Ok(responses);
    }

    [HttpGet("sessions/{id:guid}")]
    public async Task<ActionResult<MentorSessionResponse>> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var session = await dbContext.MentorSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);

        if (session is null)
        {
            return NotFound(new { message = "Mentor session was not found." });
        }

        return Ok(ToResponse(session, ParseAiResponse(session.Answer), null));
    }

    [HttpGet("quota")]
    public async Task<ActionResult<AiChatQuotaResponse>> GetQuota(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var quota = await GetAiChatQuotaAsync(userId, cancellationToken);
        return Ok(quota);
    }

    // ============================================================
    //  HELPERS
    // ============================================================

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }

    private async Task<AiChatQuotaResponse> GetAiChatQuotaAsync(Guid userId, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .Include(item => item.Plan)
            .Where(item => item.UserId == userId && item.Status == "Active")
            .OrderByDescending(item => item.StartedAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var limit = FreePlanAiChatLimit;
        var planName = "Free";
        var since = DateTimeOffset.UtcNow.AddMonths(-1); // sliding 30-day window for free users

        if (subscription is not null)
        {
            planName = subscription.Plan.Name;
            since = subscription.StartedAt ?? since;

            var parsedLimit = ParseAiChatLimit(subscription.Plan.FeaturesJson);
            if (parsedLimit.HasValue)
            {
                limit = parsedLimit.Value;
            }
        }

        // Limit = -1 means unlimited
        if (limit < 0)
        {
            return new AiChatQuotaResponse(planName, -1, 0, int.MaxValue, since);
        }

        var used = await dbContext.MentorSessions
            .AsNoTracking()
            .CountAsync(item => item.UserId == userId && item.CreatedAt >= since, cancellationToken);

        return new AiChatQuotaResponse(planName, limit, used, Math.Max(limit - used, 0), since);
    }

    private static int? ParseAiChatLimit(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson)) return null;

        try
        {
            using var doc = JsonDocument.Parse(featuresJson);
            foreach (var key in new[] { "aiChatLimit", "AiChatLimit", "ai_chat_messages_per_period" })
            {
                if (doc.RootElement.TryGetProperty(key, out var value))
                {
                    if (value.ValueKind == JsonValueKind.Number)
                    {
                        return value.GetInt32();
                    }
                    if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
                    {
                        return parsed;
                    }
                }
            }
        }
        catch
        {
            // ignore malformed json
        }

        return null;
    }

    private static string BuildSystemInstruction() => """
        You are an AI Career Mentor for software engineering students on the CareerMap platform.
        You give practical, concise, structured guidance in Vietnamese.

        You MUST respond in valid JSON with this exact shape (no Markdown fences, no extra prose outside JSON):
        {
          "answer": "Markdown text trả lời câu hỏi (bullet, code block, bold đều OK).",
          "intent": "GenerateRoadmap | RecommendSkill | Feedback | QnA",
          "suggestions": {
            "roadmap": null | {
              "title": "string",
              "description": "string",
              "careerRoleHint": "tên role mục tiêu nếu có",
              "totalEstimatedHours": number,
              "nodes": [
                {
                  "title": "string",
                  "description": "string",
                  "nodeType": "Group | Module | Resource",
                  "estimatedHours": number,
                  "priority": 1-5,
                  "orderIndex": number,
                  "children": [ ...same shape... ]
                }
              ]
            },
            "actions": [
              { "type": "ApplyRoadmap" | "RequestReview" | "OpenSection", "label": "string", "payload": object }
            ],
            "resources": [
              { "title": "string", "url": "string", "type": "Article|Course|Video|Book" }
            ]
          }
        }

        STRICT RULES on "suggestions.roadmap" (đọc kỹ):
        - DEFAULT: "suggestions.roadmap" PHẢI là null.
        - CHỈ trả roadmap khi câu hỏi của student CHỨA rõ ràng yêu cầu lập lộ trình học, ví dụ:
          • "lộ trình", "roadmap", "học theo thứ tự", "kế hoạch học", "study plan", "learning path"
          • "tôi nên học gì trước", "bắt đầu từ đâu", "học như thế nào để thành X"
          • "đề xuất lộ trình", "tạo roadmap cho tôi"
        - TUYỆT ĐỐI KHÔNG tự suy diễn để trả roadmap khi student chỉ:
          • hỏi định nghĩa / khái niệm ("React là gì?", "DI là gì?")
          • hỏi so sánh ("React vs Vue?")
          • hỏi feedback portfolio / CV
          • hỏi 1 câu kỹ thuật cụ thể ("làm sao deploy lên Vercel?")
          • chào hỏi / small talk
        - Việc student có skill gap KHÔNG phải lý do để tự đính kèm roadmap. Chỉ dùng skill gap như context để trả lời TRÚNG câu hỏi.

        Other rules:
        - If the student asks you to review or analyze their CV, use the `cvParsedText` (which contains the raw text extracted from their PDF CV) to provide specific, detailed feedback on their content, spelling, and professional impact. If `cvParsedText` is 'none', advise them to upload a CV first.
        - Base every recommendation on the student profile/skill/feedback context. Do not invent unrelated content.
        - Keep "answer" focused and ≤ 800 words.
        - Use Vietnamese for all human-readable strings.
        - When you DO return roadmap: 3-7 top-level groups, each with 2-6 child modules. Estimated hours realistic.
        - "actions" tối đa 3 items, chỉ trả khi thực sự hữu ích cho câu hỏi.
        - "resources" tối đa 5 items, chỉ URL công khai thật.

        Examples:
        Q: "React là gì?"
        → intent="QnA", suggestions.roadmap=null, actions=[], resources=[1-2 link tham khảo]

        Q: "So sánh REST và GraphQL?"
        → intent="QnA", suggestions.roadmap=null

        Q: "Cho tôi lộ trình học để trở thành Frontend Developer"
        → intent="GenerateRoadmap", suggestions.roadmap={...full tree...}, actions=[{type:"ApplyRoadmap",...}]

        Q: "Tôi nên học gì tiếp theo?"  (kèm context: profile có target Backend, skill gap 35%)
        → intent="GenerateRoadmap", suggestions.roadmap={...}, base on skill gap
        """;

    private async Task<string> BuildMentorContext(
        Guid userId,
        string? extraContext,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.StudentProfiles
            .AsNoTracking()
            .Include(item => item.TargetRole)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        var skills = await dbContext.UserSkills
            .AsNoTracking()
            .Include(item => item.Skill)
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.Skill.Category)
            .ThenBy(item => item.Skill.Name)
            .Select(item => $"{item.Skill.Name} ({item.Skill.Category}): {item.Level}, verified={item.IsVerified}")
            .ToListAsync(cancellationToken);

        var latestGap = await dbContext.SkillGapReports
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                item.Id,
                item.MatchScore,
                item.Summary,
                CareerRole = item.CareerRole.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        var gapItems = latestGap is null
            ? []
            : await dbContext.SkillGapReportItems
                .AsNoTracking()
                .Include(item => item.Skill)
                .Where(item => item.SkillGapReportId == latestGap.Id)
                .OrderBy(item => item.Priority)
                .Select(item => $"{item.Skill.Name}: current={item.CurrentLevel ?? "Missing"}, required={item.RequiredLevel}, status={item.Status}, priority={item.Priority}, recommendation={item.Recommendation}")
                .ToListAsync(cancellationToken);

        var roadmap = await dbContext.Roadmaps
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Status,
                item.Progress
            })
            .FirstOrDefaultAsync(cancellationToken);

        var roadmapNodes = roadmap is null
            ? []
            : await dbContext.RoadmapNodes
                .AsNoTracking()
                .Where(item => item.RoadmapId == roadmap.Id)
                .OrderBy(item => item.OrderIndex)
                .Select(item => $"{item.OrderIndex}. {item.Title} - {item.Status}, priority={item.Priority}, hours={item.EstimatedHours}")
                .ToListAsync(cancellationToken);

        var repos = await dbContext.GithubRepositories
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.QualityScore)
            .Take(5)
            .Select(item => $"{item.RepoName}: language={item.MainLanguage}, score={item.QualityScore}, summary={item.AiSummary}")
            .ToListAsync(cancellationToken);

        // NEW: mentor + counselor feedbacks (5 most recent each)
        var mentorFeedbacks = await dbContext.MentorFeedbacks
            .AsNoTracking()
            .Where(item => item.StudentId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(5)
            .Select(item => $"[Mentor {item.CreatedAt:yyyy-MM-dd}] rating={item.Rating}, readiness={item.JobReadinessLevel}, comment={item.Comment}, recommendations={item.Recommendations}")
            .ToListAsync(cancellationToken);

        var counselorFeedbacks = await dbContext.CounselorFeedbacks
            .AsNoTracking()
            .Where(item => item.StudentId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(5)
            .Select(item => $"[Counselor {item.CreatedAt:yyyy-MM-dd}] rating={item.Rating}, feedback={item.FeedbackText}, recommendations={item.Recommendations}")
            .ToListAsync(cancellationToken);

        // NEW: portfolio + projects
        var portfolio = await dbContext.Portfolios
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.UpdatedAt)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.IsPublished,
                item.Slug
            })
            .FirstOrDefaultAsync(cancellationToken);

        var portfolioProjects = portfolio is null
            ? []
            : await dbContext.PortfolioProjects
                .AsNoTracking()
                .Where(item => item.PortfolioId == portfolio.Id)
                .OrderBy(item => item.OrderIndex)
                .Select(item => $"{item.Title}: {item.Description}, tech={item.TechStackJson}")
                .ToListAsync(cancellationToken);

        // NEW: roadmap review history (last 10)
        var reviewHistory = await dbContext.RoadmapNodeReviewRequests
            .AsNoTracking()
            .Include(item => item.RoadmapNode)
            .Include(item => item.Reviewer)
            .Where(item => item.StudentId == userId)
            .OrderByDescending(item => item.RequestedAt)
            .Take(10)
            .Select(item => $"[{item.Status} {item.RequestedAt:yyyy-MM-dd}] node={item.RoadmapNode.Title}, reviewer={item.Reviewer.FullName} ({item.ReviewerRole}), reviewerNote={item.ReviewerNote}")
            .ToListAsync(cancellationToken);

        return $$"""
        Profile:
        school={{profile?.School ?? "unknown"}}
        major={{profile?.Major ?? "unknown"}}
        year={{profile?.Year?.ToString() ?? "unknown"}}
        gpa={{profile?.Gpa?.ToString() ?? "unknown"}}
        targetRole={{profile?.TargetRole?.Name ?? "unknown"}}
        careerGoal={{profile?.CareerGoal ?? "unknown"}}
        preferredHoursPerWeek={{profile?.PreferredLearningHoursPerWeek?.ToString() ?? "unknown"}}
        cvName={{profile?.CvName ?? "none"}}
        cvUrl={{(profile?.CvUrl != null ? $"/api/storage/download?objectName={Uri.EscapeDataString(profile.CvUrl)}" : "none")}}
        cvParsedText={{(string.IsNullOrWhiteSpace(profile?.CvParsedText) ? "none" : profile.CvParsedText)}}

        Skills:
        {{string.Join("\n", skills.DefaultIfEmpty("No skills entered."))}}

        Latest skill gap:
        role={{latestGap?.CareerRole ?? "unknown"}}
        matchScore={{latestGap?.MatchScore.ToString() ?? "unknown"}}
        summary={{latestGap?.Summary ?? "unknown"}}
        items={{string.Join("\n", gapItems.DefaultIfEmpty("No gap items."))}}

        Latest roadmap:
        {{(roadmap is null ? "No roadmap." : $"{roadmap.Title}, status={roadmap.Status}, progress={roadmap.Progress}")}}
        {{string.Join("\n", roadmapNodes.DefaultIfEmpty("No roadmap nodes."))}}

        GitHub repositories:
        {{string.Join("\n", repos.DefaultIfEmpty("No repositories analyzed."))}}

        Recent mentor feedbacks:
        {{string.Join("\n", mentorFeedbacks.DefaultIfEmpty("No mentor feedbacks."))}}

        Recent counselor feedbacks:
        {{string.Join("\n", counselorFeedbacks.DefaultIfEmpty("No counselor feedbacks."))}}

        Portfolio:
        {{(portfolio is null ? "No portfolio." : $"{portfolio.Title} (published={portfolio.IsPublished}, slug={portfolio.Slug})")}}
        {{string.Join("\n", portfolioProjects.DefaultIfEmpty("No portfolio projects."))}}

        Roadmap review history:
        {{string.Join("\n", reviewHistory.DefaultIfEmpty("No review requests yet."))}}

        Extra context:
        {{(string.IsNullOrWhiteSpace(extraContext) ? "none" : extraContext.Trim())}}
        """;
    }

    private ParsedAiResponse ParseAiResponse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new ParsedAiResponse(string.Empty, "QnA", null);
        }

        // 1) Try parse the entire string as our envelope JSON
        try
        {
            var trimmed = raw.Trim();
            // Strip optional ```json fence
            if (trimmed.StartsWith("```"))
            {
                var firstNewline = trimmed.IndexOf('\n');
                if (firstNewline > 0)
                {
                    trimmed = trimmed[(firstNewline + 1)..];
                }
                if (trimmed.EndsWith("```"))
                {
                    trimmed = trimmed[..^3].TrimEnd();
                }
            }

            // Detect envelope shape
            if (trimmed.StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.TryGetProperty("answer", out var answerElement))
                {
                    var answer = answerElement.GetString() ?? string.Empty;
                    var intent = doc.RootElement.TryGetProperty("intent", out var intentElement)
                        ? intentElement.GetString() ?? "QnA"
                        : "QnA";
                    JsonElement? suggestions = doc.RootElement.TryGetProperty("suggestions", out var sugEl)
                        ? sugEl
                        : null;

                    object? suggestionsValue = null;
                    if (suggestions.HasValue && suggestions.Value.ValueKind == JsonValueKind.Object)
                    {
                        suggestionsValue = JsonSerializer.Deserialize<JsonElement>(
                            suggestions.Value.GetRawText());
                    }

                    return new ParsedAiResponse(answer, intent, suggestionsValue);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "AI response was not valid envelope JSON; falling back to plain text.");
        }

        // 2) Fallback: treat entire raw output as the answer text
        return new ParsedAiResponse(raw, "QnA", null);
    }

    private static MentorSessionResponse ToResponse(
        MentorSession session,
        ParsedAiResponse parsed,
        AiChatQuotaResponse? quota) =>
        new(
            session.Id,
            session.Question,
            parsed.Answer,
            parsed.Intent,
            parsed.Suggestions,
            session.ContextJson,
            session.Model,
            session.TokensUsed,
            session.CreatedAt,
            quota);

    private sealed record ParsedAiResponse(string Answer, string Intent, object? Suggestions);
}

public sealed record MentorChatRequest(string Question, string? ContextJson);

public sealed record MentorSessionResponse(
    Guid Id,
    string Question,
    string Answer,
    string Intent,
    object? Suggestions,
    string? ContextJson,
    string? Model,
    int? TokensUsed,
    DateTimeOffset CreatedAt,
    AiChatQuotaResponse? Quota);

public sealed record AiChatQuotaResponse(
    string PlanName,
    int Limit,
    int Used,
    int Remaining,
    DateTimeOffset Since);

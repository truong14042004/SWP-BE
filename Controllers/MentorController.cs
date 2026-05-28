using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
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
    private sealed record ReferenceResource(string Keyword, string Title, string Url, string Type);

    private const int FreePlanAiChatLimit = 20;

    private static readonly ReferenceResource[] MentorReferenceCatalog = [
        new("AI API Integration", "OpenAI API documentation", "https://platform.openai.com/docs", "Article"),
        new("Prompt Engineering", "OpenAI prompt engineering guide", "https://platform.openai.com/docs/guides/prompt-engineering", "Article"),
        new("Authentication and Authorization", "Microsoft Learn: ASP.NET Core authentication", "https://learn.microsoft.com/en-us/aspnet/core/security/authentication/", "Article"),
        new("Backend", "Microsoft Learn: Build web APIs with ASP.NET Core", "https://learn.microsoft.com/en-us/training/modules/build-web-api-aspnet-core/", "Course"),
        new("REST API Design", "Microsoft REST API Guidelines", "https://github.com/microsoft/api-guidelines", "Article"),
        new("Database Design", "Microsoft Learn: EF Core modeling", "https://learn.microsoft.com/en-us/ef/core/modeling/", "Article"),
        new("SQL", "SQLBolt interactive SQL lessons", "https://sqlbolt.com/", "Course"),
        new("Unit Testing", "Microsoft Learn: Unit testing C#", "https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test", "Article"),
        new("Test Automation", "Playwright documentation", "https://playwright.dev/docs/intro", "Article"),
        new("React", "React documentation", "https://react.dev/learn", "Article"),
        new("TypeScript", "TypeScript handbook", "https://www.typescriptlang.org/docs/handbook/intro.html", "Book"),
        new("Responsive UI", "MDN responsive design", "https://developer.mozilla.org/en-US/docs/Learn_web_development/Core/CSS_layout/Responsive_Design", "Article"),
        new("API Integration", "MDN Fetch API", "https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API", "Article"),
        new("Docker", "Docker Get Started", "https://docs.docker.com/get-started/", "Article"),
        new("CI/CD", "GitHub Actions documentation", "https://docs.github.com/en/actions", "Article"),
        new("Cloud Deployment", "Google Cloud Run documentation", "https://cloud.google.com/run/docs", "Article"),
        new("Data Pipeline", "Microsoft Learn: Data engineering", "https://learn.microsoft.com/en-us/training/paths/data-engineer-azure-databricks/", "Course"),
        new("GitHub Portfolio", "GitHub Docs: About READMEs", "https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes", "Article"),
        new("Mobile UI", "Material Design mobile guidelines", "https://m3.material.io/", "Article"),
        new("Kỹ năng Thuyết trình", "Harvard ManageMentor: Presentation skills", "https://www.harvardbusiness.org/insight/presentation-skills/", "Article")
    ];

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
            return BadRequest(new { message = "Câu hỏi là bắt buộc." });
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

        MentorSession? existingSession = null;
        IReadOnlyList<MentorChatMessageResponse> existingMessages = [];
        if (request.SessionId is Guid sessionId)
        {
            existingSession = await dbContext.MentorSessions
                .SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId, cancellationToken);
            if (existingSession is null)
            {
                return NotFound(new { message = "Không tìm thấy phiên tư vấn." });
            }

            existingMessages = GetSessionMessages(existingSession, ParseAiResponse(existingSession.Answer));
        }

        var context = await BuildMentorContext(userId, request.ContextJson, cancellationToken);
        var previousMessagesText = existingMessages.Count == 0
            ? "No previous messages in this thread."
            : string.Join("\n", existingMessages.Select(item => $"{item.Role}: {item.Content}"));

        AiTextResult aiResult;
        try
        {
            aiResult = await aiTextGenerationService.GenerateAsync(
                BuildSystemInstruction(),
                $"""
                Student context:
                {context}

                Previous messages in this mentor thread:
                {previousMessagesText}

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
        var parsed = await SanitizeParsedAiResponseAsync(ParseAiResponse(aiResult.Text), cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var sessionPayload = new
        {
            answer = parsed.Answer,
            intent = parsed.Intent,
            suggestions = parsed.Suggestions,
            messages = existingMessages
                .Concat([
                    new MentorChatMessageResponse(
                        $"user-{now.ToUnixTimeMilliseconds()}",
                        "user",
                        request.Question.Trim(),
                        now,
                        null,
                        null,
                        null),
                    new MentorChatMessageResponse(
                        $"assistant-{now.ToUnixTimeMilliseconds()}",
                        "assistant",
                        parsed.Answer,
                        now,
                        aiResult.TokensUsed,
                        aiResult.Model,
                        parsed.Intent,
                        parsed.Suggestions)
                ])
                .ToList()
        };
        var payloadJson = JsonSerializer.Serialize(sessionPayload, LooseJson);

        var session = existingSession ?? new MentorSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Question = request.Question.Trim(),
            CreatedAt = now
        };
        session.Answer = payloadJson;
        session.ContextJson = context;
        session.Model = aiResult.Model;
        session.TokensUsed = (session.TokensUsed ?? 0) + (aiResult.TokensUsed ?? 0);

        try
        {
            if (existingSession is null)
            {
                dbContext.MentorSessions.Add(session);
            }
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
            return NotFound(new { message = "Không tìm thấy phiên cố vấn." });
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
            : throw new UnauthorizedAccessException("Token người dùng không hợp lệ.");
    }

    private async Task<AiChatQuotaResponse> GetAiChatQuotaAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .Include(item => item.Plan)
            .Where(item => item.UserId == userId && (item.Status == "Active" || (item.Status == "Cancelled" && item.ExpiredAt > now)))
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
                var baseLimit = parsedLimit.Value;
                if (baseLimit < 0)
                {
                    limit = -1;
                }
                else if (subscription.StartedAt.HasValue && subscription.ExpiredAt.HasValue)
                {
                    var billingCycle = subscription.Plan.BillingCycle ?? "Monthly";
                    var periods = CalculatePeriods(subscription.StartedAt.Value, subscription.ExpiredAt.Value, billingCycle);
                    limit = baseLimit * periods;
                }
                else
                {
                    limit = baseLimit;
                }
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
        - Roadmap modules/resources MUST come only from the "Database learning resources" and "Database skill categories" sections in Student context. Do not invent roadmap modules, courses, lessons, or applied learning items that are not listed there.
        - suggestions.resources is only a reference-links section for the chat answer. It MAY include external public links when useful, preferably official documentation or reputable learning references. Do not treat these links as roadmap modules.
        - If you return suggestions.roadmap, top-level group titles MUST exactly match one of the Skill.Category values in "Database skill categories". Module titles MUST exactly match a Skill.Name or LearningResource.Title listed in context.
        - Keep "answer" focused and ≤ 800 words.
        - Use Vietnamese for all human-readable strings.
        - When you DO return roadmap: use only existing database skill categories as top-level groups, each with existing database skills/resources as child modules. Estimated hours realistic.
        - "actions" tối đa 3 items, chỉ trả khi thực sự hữu ích cho câu hỏi.
        - Luon tra "resources" 1-5 items khi cau tra loi co kien thuc ky thuat hoac roadmap. Resources co the la link tham khao ben ngoai; moi item can co title va url hop le.

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

        var databaseSkillCategories = await dbContext.Skills
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Name)
            .Select(item => $"{item.Category} | skill={item.Name}")
            .ToListAsync(cancellationToken);

        var databaseResources = await dbContext.LearningResources
            .AsNoTracking()
            .Include(item => item.Skill)
            .Where(item => item.IsActive)
            .OrderBy(item => item.Skill == null ? "" : item.Skill.Category)
            .ThenBy(item => item.Skill == null ? "" : item.Skill.Name)
            .ThenBy(item => item.LessonNumber)
            .Take(80)
            .Select(item => $"{item.Title} | skill={(item.Skill == null ? "none" : item.Skill.Name)} | category={(item.Skill == null ? "none" : item.Skill.Category)} | type={item.ResourceType} | difficulty={(item.Difficulty ?? "unknown")} | hours={item.EstimatedHours} | url={item.Url}")
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

        Database skill categories:
        {{string.Join("\n", databaseSkillCategories.DefaultIfEmpty("No active skills in database."))}}

        Database learning resources:
        {{string.Join("\n", databaseResources.DefaultIfEmpty("No active learning resources in database."))}}

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

    private async Task<ParsedAiResponse> SanitizeParsedAiResponseAsync(
        ParsedAiResponse parsed,
        CancellationToken cancellationToken)
    {
        if (parsed.Suggestions is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return parsed;
        }

        var categories = await dbContext.Skills
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => item.Category)
            .Distinct()
            .ToListAsync(cancellationToken);
        var categorySet = categories.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var skillTitles = await dbContext.Skills
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => item.Name)
            .ToListAsync(cancellationToken);
        var allowedModuleTitles = skillTitles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var resources = await dbContext.LearningResources
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => new { item.Title, item.Url })
            .ToListAsync(cancellationToken);
        foreach (var resource in resources)
        {
            allowedModuleTitles.Add(resource.Title);
        }

        var suggestions = JsonNode.Parse(element.GetRawText()) as JsonObject;
        if (suggestions is null)
        {
            return parsed;
        }

        if (suggestions["resources"] is JsonArray resourcesArray)
        {
            suggestions["resources"] = SanitizeReferenceResources(resourcesArray);
        }

        if (suggestions["roadmap"] is JsonObject roadmap && roadmap["nodes"] is JsonArray nodes)
        {
            var sanitizedTopLevel = SanitizeRoadmapCategories(nodes, categorySet, allowedModuleTitles);
            roadmap["nodes"] = sanitizedTopLevel;
            if (sanitizedTopLevel.Count == 0)
            {
                suggestions["roadmap"] = null;
            }
        }

        if (suggestions["resources"] is not JsonArray sanitizedResources || sanitizedResources.Count == 0)
        {
            suggestions["resources"] = BuildFallbackReferenceResources(suggestions, parsed.Answer);
        }

        return parsed with
        {
            Suggestions = JsonSerializer.Deserialize<JsonElement>(suggestions.ToJsonString())
        };
    }

    private static JsonArray SanitizeReferenceResources(JsonArray resources)
    {
        var sanitized = new JsonArray();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in resources)
        {
            if (sanitized.Count >= 5) break;
            if (node is not JsonObject obj) continue;

            var title = obj["title"]?.GetValue<string>()?.Trim();
            var url = obj["url"]?.GetValue<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(title)
                || string.IsNullOrWhiteSpace(url)
                || !IsAllowedReferenceUrl(url)
                || !seenUrls.Add(url))
            {
                continue;
            }

            sanitized.Add(new JsonObject
            {
                ["title"] = title.Length > 200 ? title[..200] : title,
                ["url"] = url,
                ["type"] = NormalizeReferenceType(obj["type"]?.GetValue<string>())
            });
        }

        return sanitized;
    }

    private static JsonArray BuildFallbackReferenceResources(JsonObject suggestions, string answer)
    {
        var titles = new List<string>();
        CollectSuggestionTitles(suggestions["roadmap"], titles);

        var searchText = string.Join('\n', titles.Append(answer ?? string.Empty));
        var resources = new JsonArray();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var reference in MentorReferenceCatalog)
        {
            if (resources.Count >= 5) break;
            if (!searchText.Contains(reference.Keyword, StringComparison.OrdinalIgnoreCase)) continue;
            if (!seenUrls.Add(reference.Url)) continue;

            resources.Add(new JsonObject
            {
                ["title"] = reference.Title,
                ["url"] = reference.Url,
                ["type"] = reference.Type
            });
        }

        if (resources.Count == 0)
        {
            resources.Add(new JsonObject
            {
                ["title"] = "Microsoft Learn",
                ["url"] = "https://learn.microsoft.com/en-us/training/",
                ["type"] = "Course"
            });
        }

        return resources;
    }

    private static void CollectSuggestionTitles(JsonNode? node, List<string> titles)
    {
        switch (node)
        {
            case JsonObject obj:
                var title = obj["title"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    titles.Add(title);
                }
                CollectSuggestionTitles(obj["nodes"], titles);
                CollectSuggestionTitles(obj["children"], titles);
                break;
            case JsonArray array:
                foreach (var child in array)
                {
                    CollectSuggestionTitles(child, titles);
                }
                break;
        }
    }

    private static bool IsAllowedReferenceUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme == Uri.UriSchemeHttps || absoluteUri.Scheme == Uri.UriSchemeHttp;
        }

        return Uri.TryCreate(url, UriKind.Relative, out _)
            && url.StartsWith('/');
    }

    private static string NormalizeReferenceType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return "Article";

        return type.Trim() switch
        {
            var value when value.Equals("Course", StringComparison.OrdinalIgnoreCase) => "Course",
            var value when value.Equals("Video", StringComparison.OrdinalIgnoreCase) => "Video",
            var value when value.Equals("Book", StringComparison.OrdinalIgnoreCase) => "Book",
            _ => "Article"
        };
    }

    private static JsonArray SanitizeRoadmapCategories(
        JsonArray nodes,
        HashSet<string> categorySet,
        HashSet<string> allowedModuleTitles)
    {
        var sanitized = new JsonArray();
        foreach (var node in nodes)
        {
            if (node is not JsonObject obj) continue;
            var title = obj["title"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(title) || !categorySet.Contains(title)) continue;

            var clone = (JsonObject)obj.DeepClone();
            if (clone["children"] is JsonArray children)
            {
                clone["children"] = SanitizeRoadmapModules(children, allowedModuleTitles);
            }
            sanitized.Add(clone);
        }
        return sanitized;
    }

    private static JsonArray SanitizeRoadmapModules(
        JsonArray nodes,
        HashSet<string> allowedModuleTitles)
    {
        var sanitized = new JsonArray();
        foreach (var node in nodes)
        {
            if (node is not JsonObject obj) continue;
            var title = obj["title"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(title)) continue;

            var sanitizedChildren = obj["children"] is JsonArray children
                ? SanitizeRoadmapModules(children, allowedModuleTitles)
                : [];

            if (!allowedModuleTitles.Contains(title))
            {
                foreach (var child in sanitizedChildren)
                {
                    sanitized.Add(child?.DeepClone());
                }
                continue;
            }

            var clone = (JsonObject)obj.DeepClone();
            if (sanitizedChildren.Count > 0)
            {
                clone["children"] = sanitizedChildren;
            }
            else
            {
                clone.Remove("children");
            }
            sanitized.Add(clone);
        }
        return sanitized;
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
            quota,
            GetSessionMessages(session, parsed));

    private static IReadOnlyList<MentorChatMessageResponse> GetSessionMessages(
        MentorSession session,
        ParsedAiResponse parsed)
    {
        try
        {
            using var doc = JsonDocument.Parse(session.Answer);
            if (doc.RootElement.TryGetProperty("messages", out var messagesElement)
                && messagesElement.ValueKind == JsonValueKind.Array)
            {
                var messages = JsonSerializer.Deserialize<List<MentorChatMessageResponse>>(
                    messagesElement.GetRawText(),
                    LooseJson);
                if (messages is { Count: > 0 })
                {
                    return messages;
                }
            }
        }
        catch
        {
            // Older rows may contain plain text or an envelope without transcript messages.
        }

        return [
            new MentorChatMessageResponse(
                $"{session.Id}-question",
                "user",
                session.Question,
                session.CreatedAt,
                null,
                null,
                null),
            new MentorChatMessageResponse(
                $"{session.Id}-answer",
                "assistant",
                parsed.Answer,
                session.CreatedAt,
                session.TokensUsed,
                session.Model,
                parsed.Intent,
                parsed.Suggestions)
        ];
    }

    private sealed record ParsedAiResponse(string Answer, string Intent, object? Suggestions);

    private static int CalculatePeriods(DateTimeOffset start, DateTimeOffset end, string billingCycle)
    {
        if (billingCycle.Equals("Free", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        var months = (end.Year - start.Year) * 12 + end.Month - start.Month;
        if (months <= 0)
        {
            return 1;
        }

        if (billingCycle.Equals("Yearly", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Max(1, months / 12);
        }
        if (billingCycle.Equals("Quarterly", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Max(1, months / 3);
        }

        // Default to Monthly
        return months;
    }
}

public sealed record MentorChatRequest(string Question, string? ContextJson, Guid? SessionId);

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
    AiChatQuotaResponse? Quota,
    IReadOnlyList<MentorChatMessageResponse> Messages);

public sealed record MentorChatMessageResponse(
    string Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    int? TokensUsed,
    string? Model,
    string? Intent,
    object? Suggestions = null);

public sealed record AiChatQuotaResponse(
    string PlanName,
    int Limit,
    int Used,
    int Remaining,
    DateTimeOffset Since);

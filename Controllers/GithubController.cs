using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWP_BE.Data;
using SWP_BE.Models;
using SWP_BE.Options;
using SWP_BE.Services;

namespace SWP_BE.Controllers;

[ApiController]
[Authorize]
[Route("api/github")]
public sealed class GithubController(
    AppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IOptions<GithubOptions> githubOptions,
    IOptions<GithubOAuthOptions> githubOAuthOptions,
    IConfiguration configuration,
    IAiTextGenerationService aiTextGenerationService) : ControllerBase
{
    private readonly GithubOptions _githubOptions = githubOptions.Value;
    private readonly GithubOAuthOptions _githubOAuthOptions = githubOAuthOptions.Value;

    [HttpPost("oauth/login")]
    public async Task<ActionResult<GithubOAuthLoginResponse>> CreateOAuthLogin(
        GithubOAuthLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_githubOAuthOptions.ClientId)
            || string.IsNullOrWhiteSpace(_githubOAuthOptions.ClientSecret)
            || string.IsNullOrWhiteSpace(_githubOAuthOptions.CallbackUrl))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "GitHub OAuth configuration is missing."
            });
        }

        var userId = GetCurrentUserId();
        var now = DateTimeOffset.UtcNow;
        var state = CreateOAuthState();
        var returnUrl = ResolveOAuthReturnUrl(request.ReturnUrl);

        dbContext.GithubOAuthStates.Add(new GithubOAuthState
        {
            State = state,
            UserId = userId,
            ReturnUrl = returnUrl,
            ExpiresAt = now.AddMinutes(10),
            CreatedAt = now
        });

        await dbContext.GithubOAuthStates
            .Where(item => item.UserId == userId && item.ExpiresAt < now)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var scope = string.IsNullOrWhiteSpace(request.Scope)
            ? "repo read:user user:email"
            : request.Scope.Trim();

        var parameters = new Dictionary<string, string?>
        {
            ["client_id"] = _githubOAuthOptions.ClientId,
            ["redirect_uri"] = _githubOAuthOptions.CallbackUrl,
            ["scope"] = scope,
            ["state"] = state,
            ["allow_signup"] = "true"
        };

        var authorizationUrl = QueryHelpers.AddQueryString(
            "https://github.com/login/oauth/authorize",
            parameters);

        return Ok(new GithubOAuthLoginResponse(authorizationUrl, state, returnUrl));
    }

    [AllowAnonymous]
    [HttpGet("oauth/callback")]
    public async Task<IActionResult> OAuthCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return BadRequest(new { message = "GitHub OAuth was rejected.", error });
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return BadRequest(new { message = "GitHub OAuth callback is missing code or state." });
        }

        var now = DateTimeOffset.UtcNow;
        var oauthState = await dbContext.GithubOAuthStates
            .SingleOrDefaultAsync(item => item.State == state, cancellationToken);
        if (oauthState is null || oauthState.ExpiresAt < now)
        {
            return BadRequest(new { message = "GitHub OAuth state is invalid or expired." });
        }

        var token = await ExchangeCodeForTokenAsync(code, cancellationToken);
        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            return BadRequest(new { message = "Could not exchange GitHub OAuth code for an access token." });
        }

        var client = CreateGithubClient(token.AccessToken);
        var githubUser = await client.GetFromJsonAsync<GithubUserProfileDto>(
            "https://api.github.com/user",
            cancellationToken);
        if (githubUser is null || string.IsNullOrWhiteSpace(githubUser.Login))
        {
            return BadRequest(new { message = "Could not read GitHub user profile." });
        }

        var connection = await dbContext.GithubConnections
            .SingleOrDefaultAsync(item => item.UserId == oauthState.UserId, cancellationToken);
        if (connection is null)
        {
            connection = new GithubConnection
            {
                Id = Guid.NewGuid(),
                UserId = oauthState.UserId,
                ConnectedAt = now
            };
            dbContext.GithubConnections.Add(connection);
        }

        connection.GithubUserId = githubUser.Id;
        connection.GithubUsername = githubUser.Login;
        connection.AccessToken = token.AccessToken;
        connection.TokenType = string.IsNullOrWhiteSpace(token.TokenType) ? "bearer" : token.TokenType;
        connection.Scope = token.Scope;
        connection.UpdatedAt = now;

        var profile = await dbContext.StudentProfiles
            .SingleOrDefaultAsync(item => item.UserId == oauthState.UserId, cancellationToken);
        if (profile is not null)
        {
            profile.GithubUsername = githubUser.Login;
            profile.UpdatedAt = now;
        }

        dbContext.GithubOAuthStates.Remove(oauthState);
        await dbContext.SaveChangesAsync(cancellationToken);

        var returnUrl = string.IsNullOrWhiteSpace(oauthState.ReturnUrl)
            ? ResolveOAuthReturnUrl(null)
            : oauthState.ReturnUrl;
        return Redirect(QueryHelpers.AddQueryString(returnUrl, "github", "connected"));
    }

    [HttpPost("sync")]
    public async Task<ActionResult<IReadOnlyList<GithubRepositoryResponse>>> Sync(
        GithubSyncRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var githubConnection = await dbContext.GithubConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var useOAuthConnection = githubConnection is not null
            && (request.IncludePrivate == true || string.IsNullOrWhiteSpace(request.Username));
        var username = request.Username?.Trim();
        if (useOAuthConnection)
        {
            username = githubConnection!.GithubUsername;
        }
        else if (string.IsNullOrWhiteSpace(username))
        {
            username = await dbContext.StudentProfiles
                .Where(profile => profile.UserId == userId)
                .Select(profile => profile.GithubUsername)
                .SingleOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { message = "GitHub username is required." });
        }

        var client = CreateGithubClient(useOAuthConnection ? githubConnection!.AccessToken : null);

        var response = await client.GetAsync(
            useOAuthConnection
                ? "https://api.github.com/user/repos?per_page=100&sort=updated&visibility=all&affiliation=owner,collaborator,organization_member"
                : $"https://api.github.com/users/{Uri.EscapeDataString(username)}/repos?per_page=100&sort=updated",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                message = "GitHub API rate limit exceeded. Add GitHub:Token or wait before retrying."
            });
        }

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, new
            {
                message = "Could not read GitHub repositories.",
                detail = await response.Content.ReadAsStringAsync(cancellationToken)
            });
        }

        var repos = await response.Content.ReadFromJsonAsync<List<GithubRepoDto>>(cancellationToken);

        if (repos is null)
        {
            return BadRequest(new { message = "Could not read GitHub repositories." });
        }

        var now = DateTimeOffset.UtcNow;
        var existing = await dbContext.GithubRepositories
            .Where(repository => repository.UserId == userId)
            .ToDictionaryAsync(repository => repository.RepoUrl, cancellationToken);

        foreach (var repo in repos.Where(repo => !string.IsNullOrWhiteSpace(repo.HtmlUrl)))
        {
            if (!existing.TryGetValue(repo.HtmlUrl, out var repository))
            {
                repository = new GithubRepository
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RepoUrl = repo.HtmlUrl,
                    CreatedAt = now
                };
                dbContext.GithubRepositories.Add(repository);
            }

            repository.RepoName = repo.Name;
            repository.Description = repo.Description;
            repository.MainLanguage = repo.Language;
            repository.LastSyncedAt = now;
            repository.UpdatedAt = now;
        }

        var profile = await dbContext.StudentProfiles
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (profile is not null)
        {
            profile.GithubUsername = username;
            profile.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await GetRepositoryResponses(userId, cancellationToken));
    }

    [HttpGet("repositories")]
    public async Task<ActionResult<IReadOnlyList<GithubRepositoryResponse>>> GetRepositories(
        CancellationToken cancellationToken)
    {
        return Ok(await GetRepositoryResponses(GetCurrentUserId(), cancellationToken));
    }

    [HttpPost("analyze-readme")]
    public async Task<ActionResult<GithubRepositoryResponse>> AnalyzeReadme(
        AnalyzeReadmeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var repository = request.RepositoryId is not null
            ? await dbContext.GithubRepositories.SingleOrDefaultAsync(
                item => item.Id == request.RepositoryId && item.UserId == userId,
                cancellationToken)
            : null;

        if (repository is null && !string.IsNullOrWhiteSpace(request.RepoUrl))
        {
            repository = await dbContext.GithubRepositories.SingleOrDefaultAsync(
                item => item.RepoUrl == request.RepoUrl.Trim() && item.UserId == userId,
                cancellationToken);
        }

        if (repository is null)
        {
            return NotFound(new { message = "GitHub repository was not found." });
        }

        var readme = request.ReadmeContent?.Trim();
        if (string.IsNullOrWhiteSpace(readme))
        {
            readme = repository.ReadmeContent;
        }

        if (string.IsNullOrWhiteSpace(readme))
        {
            readme = await FetchReadmeAsync(
                repository.RepoUrl,
                cancellationToken,
                await GetGithubAccessTokenAsync(userId, cancellationToken));
        }

        if (string.IsNullOrWhiteSpace(readme))
        {
            return BadRequest(new { message = "README content was not found. Send readmeContent or use a public GitHub repository with README." });
        }

        var accessToken = await GetGithubAccessTokenAsync(userId, cancellationToken);
        var snapshot = await FetchRepositorySnapshotAsync(repository.RepoUrl, readme, cancellationToken, accessToken);
        AiTextResult aiResult;
        try
        {
            aiResult = await aiTextGenerationService.GenerateAsync(
                """
                You are a senior software engineering portfolio reviewer.
                Analyze a public GitHub repository using README, repository metadata, language stats, file tree, selected source/config files, and recent commit history when available.
                Return ONLY valid JSON. Do not wrap the JSON in markdown.
                Use Vietnamese for all human-readable values.
                Do not claim you inspected files that are not present in the snapshot.
                portfolioReadinessScore must be an integer from 0 to 100.
                JSON schema:
                {
                  "projectPurpose": "string",
                  "architecture": "string",
                  "techStack": ["string"],
                  "codeQuality": {
                    "summary": "string",
                    "strengths": ["string"],
                    "risks": ["string"]
                  },
                  "missingPieces": ["string"],
                  "portfolioReadinessScore": 0,
                  "priorityImprovements": [
                    {
                      "priority": 1,
                      "title": "string",
                      "reason": "string",
                      "suggestedAction": "string"
                    }
                  ],
                  "evidence": {
                    "filesReviewed": ["string"],
                    "importantFindings": ["string"]
                  }
                }
                """,
                $"""
                Repository:
                name={repository.RepoName}
                url={repository.RepoUrl}
                description={repository.Description}
                mainLanguage={repository.MainLanguage}
                defaultBranch={snapshot.DefaultBranch}
                languageStats={JsonSerializer.Serialize(snapshot.Languages)}
                totalFilesInTree={snapshot.TotalFiles}
                analyzedFiles={string.Join(", ", snapshot.Files.Select(file => file.Path))}
                recentCommits:
                {BuildCommitHistoryPrompt(snapshot.RecentCommits)}

                README:
                {TrimForPrompt(readme, 12000)}

                Repository file tree sample:
                {string.Join("\n", snapshot.TreeSample)}

                Selected source/config files:
                {BuildFilesPrompt(snapshot.Files)}
                """,
                cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = exception.Message });
        }

        var technologies = DetectTechnologies(readme)
            .Concat(snapshot.Languages.Keys)
            .Concat(DetectTechnologies(BuildFilesPrompt(snapshot.Files)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

        repository.ReadmeContent = readme;
        repository.TechStackJson = JsonSerializer.Serialize(new
        {
            technologies,
            languages = snapshot.Languages,
            analyzedFiles = snapshot.Files.Select(file => new
            {
                file.Path,
                file.Size,
                file.Truncated
            }),
            totalFiles = snapshot.TotalFiles,
            defaultBranch = snapshot.DefaultBranch,
            recentCommits = snapshot.RecentCommits
        });
        repository.QualityScore = CalculateRepositoryScore(readme, snapshot);
        repository.AiSummary = NormalizeAiJson(aiResult.Text);
        repository.UpdatedAt = DateTimeOffset.UtcNow;

        await MapRepositorySkills(repository.Id, technologies, aiResult.Text, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(repository));
    }

    private async Task<IReadOnlyList<GithubRepositoryResponse>> GetRepositoryResponses(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.GithubRepositories
            .AsNoTracking()
            .Where(repository => repository.UserId == userId)
            .OrderBy(repository => repository.RepoName)
            .Select(repository => ToResponse(repository))
            .ToListAsync(cancellationToken);
    }

    private async Task MapRepositorySkills(
        Guid repositoryId,
        IReadOnlyCollection<string> technologies,
        string evidenceText,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.GithubRepositorySkills
            .Where(item => item.GithubRepositoryId == repositoryId)
            .ToListAsync(cancellationToken);
        dbContext.GithubRepositorySkills.RemoveRange(existing);

        if (technologies.Count == 0)
        {
            return;
        }

        var normalizedTechnologies = technologies
            .Select(NormalizeSkillName)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var skills = await dbContext.Skills
            .Where(skill => skill.IsActive)
            .ToListAsync(cancellationToken);

        var matchedSkills = skills
            .Where(skill => normalizedTechnologies.Contains(NormalizeSkillName(skill.Name))
                || normalizedTechnologies.Contains(NormalizeSkillName(skill.Category)))
            .GroupBy(skill => skill.Id)
            .Select(group => group.First())
            .ToList();

        dbContext.GithubRepositorySkills.AddRange(matchedSkills.Select(skill => new GithubRepositorySkill
        {
            Id = Guid.NewGuid(),
            GithubRepositoryId = repositoryId,
            SkillId = skill.Id,
            ConfidenceScore = 85,
            EvidenceText = TrimForPrompt(evidenceText, 2000),
            CreatedAt = DateTimeOffset.UtcNow
        }));
    }

    private static string NormalizeSkillName(string value) =>
        value.Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("#", "sharp", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToLowerInvariant();

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user token.");
    }

    private async Task<string?> GetGithubAccessTokenAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.GithubConnections
            .AsNoTracking()
            .Where(connection => connection.UserId == userId)
            .Select(connection => connection.AccessToken)
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<GithubOAuthTokenDto> ExchangeCodeForTokenAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _githubOAuthOptions.ClientId,
            ["client_secret"] = _githubOAuthOptions.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = _githubOAuthOptions.CallbackUrl
        });

        var response = await client.PostAsync(
            "https://github.com/login/oauth/access_token",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new GithubOAuthTokenDto();
        }

        return await response.Content.ReadFromJsonAsync<GithubOAuthTokenDto>(cancellationToken)
            ?? new GithubOAuthTokenDto();
    }

    private string ResolveOAuthReturnUrl(string? requestedReturnUrl)
    {
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? [];
        var allowedOrigins = configuredOrigins
            .Append("https://swp-fe-careermap-2026-47ca0.web.app")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(requestedReturnUrl)
            && Uri.TryCreate(requestedReturnUrl.Trim(), UriKind.Absolute, out var requestedUri)
            && allowedOrigins.Any(origin => requestedReturnUrl.Trim().StartsWith(origin, StringComparison.OrdinalIgnoreCase)))
        {
            return requestedUri.ToString();
        }

        return allowedOrigins.FirstOrDefault() ?? "/";
    }

    private static string CreateOAuthState()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private static List<string> DetectTechnologies(string readme)
    {
        var keywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["asp.net"] = "ASP.NET Core",
            ["dotnet"] = ".NET",
            [".net"] = ".NET",
            ["react"] = "React",
            ["vite"] = "Vite",
            ["typescript"] = "TypeScript",
            ["javascript"] = "JavaScript",
            ["postgres"] = "PostgreSQL",
            ["sql server"] = "SQL Server",
            ["docker"] = "Docker",
            ["jwt"] = "JWT",
            ["entity framework"] = "Entity Framework Core",
            ["node.js"] = "Node.js",
            ["express"] = "Express",
            ["python"] = "Python",
            ["firebase"] = "Firebase"
        };

        return keywords
            .Where(pair => readme.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private HttpClient CreateGithubClient(string? accessToken = null)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SWP-BE", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        var token = string.IsNullOrWhiteSpace(accessToken)
            ? _githubOptions.Token
            : accessToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
        }

        return client;
    }

    private async Task<string?> FetchReadmeAsync(
        string repoUrl,
        CancellationToken cancellationToken,
        string? accessToken = null)
    {
        if (!TryParseGithubRepo(repoUrl, out var owner, out var repo))
        {
            return null;
        }

        var client = CreateGithubClient(accessToken);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw"));
        var response = await client.GetAsync(
            $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/readme",
            cancellationToken);

        return response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync(cancellationToken)
            : null;
    }

    private async Task<Dictionary<string, long>> FetchLanguagesAsync(
        string repoUrl,
        CancellationToken cancellationToken,
        string? accessToken = null)
    {
        if (!TryParseGithubRepo(repoUrl, out var owner, out var repo))
        {
            return [];
        }

        var client = CreateGithubClient(accessToken);
        return await client.GetFromJsonAsync<Dictionary<string, long>>(
            $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/languages",
            cancellationToken) ?? [];
    }

    private async Task<RepositorySnapshot> FetchRepositorySnapshotAsync(
        string repoUrl,
        string readme,
        CancellationToken cancellationToken,
        string? accessToken = null)
    {
        if (!TryParseGithubRepo(repoUrl, out var owner, out var repo))
        {
            return new RepositorySnapshot("unknown", new Dictionary<string, long>(), [], [], [], 0);
        }

        var client = CreateGithubClient(accessToken);
        var metadata = await client.GetFromJsonAsync<GithubRepositoryMetadataDto>(
            $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}",
            cancellationToken);
        var defaultBranch = string.IsNullOrWhiteSpace(metadata?.DefaultBranch)
            ? "main"
            : metadata.DefaultBranch;

        var languages = await FetchLanguagesAsync(repoUrl, cancellationToken, accessToken);
        var recentCommits = await FetchRecentCommitsAsync(owner, repo, cancellationToken, accessToken);
        var treeResponse = await client.GetFromJsonAsync<GithubTreeResponseDto>(
            $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/git/trees/{Uri.EscapeDataString(defaultBranch)}?recursive=1",
            cancellationToken);

        var tree = treeResponse?.Tree?
            .Where(item => item.Type == "blob" && !string.IsNullOrWhiteSpace(item.Path))
            .ToList() ?? [];

        var selected = SelectFilesForAnalysis(tree);
        var files = new List<RepositoryFileSnapshot>();
        foreach (var item in selected)
        {
            if (string.IsNullOrWhiteSpace(item.Sha))
            {
                continue;
            }

            var content = await FetchBlobTextAsync(owner, repo, item.Sha, cancellationToken, accessToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            files.Add(new RepositoryFileSnapshot(
                item.Path,
                item.Size,
                TrimForPrompt(content, 12000),
                content.Length > 12000));
        }

        if (!files.Any(file => file.Path.Contains("readme", StringComparison.OrdinalIgnoreCase)))
        {
            files.Insert(0, new RepositoryFileSnapshot("README", readme.Length, TrimForPrompt(readme, 8000), readme.Length > 8000));
        }

        return new RepositorySnapshot(
            defaultBranch,
            languages,
            recentCommits,
            tree.Select(item => item.Path).Take(250).ToList(),
            files,
            tree.Count);
    }

    private async Task<IReadOnlyList<RepositoryCommitSnapshot>> FetchRecentCommitsAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken,
        string? accessToken = null)
    {
        var client = CreateGithubClient(accessToken);
        var response = await client.GetAsync(
            $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/commits?per_page=30",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var commits = await response.Content.ReadFromJsonAsync<List<GithubCommitDto>>(cancellationToken);
        return commits?
            .Where(commit => !string.IsNullOrWhiteSpace(commit.Sha))
            .Select(commit => new RepositoryCommitSnapshot(
                commit.Sha.Length > 7 ? commit.Sha[..7] : commit.Sha,
                commit.Commit?.Message?.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty,
                commit.Commit?.Author?.Date,
                commit.Author?.Login))
            .Where(commit => !string.IsNullOrWhiteSpace(commit.Message))
            .ToList() ?? [];
    }

    private async Task<string?> FetchBlobTextAsync(
        string owner,
        string repo,
        string sha,
        CancellationToken cancellationToken,
        string? accessToken = null)
    {
        var client = CreateGithubClient(accessToken);
        var blob = await client.GetFromJsonAsync<GithubBlobDto>(
            $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/git/blobs/{Uri.EscapeDataString(sha)}",
            cancellationToken);

        if (blob is null
            || !blob.Encoding.Equals("base64", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(blob.Content))
        {
            return null;
        }

        try
        {
            var normalized = blob.Content.Replace("\n", string.Empty).Replace("\r", string.Empty);
            var bytes = Convert.FromBase64String(normalized);
            if (bytes.Length == 0 || bytes.Length > 256_000 || bytes.Any(value => value == 0))
            {
                return null;
            }

            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    private static List<GithubTreeItemDto> SelectFilesForAnalysis(IReadOnlyList<GithubTreeItemDto> tree)
    {
        var excludedSegments = new[]
        {
            "/bin/", "/obj/", "/node_modules/", "/dist/", "/build/", "/.git/",
            "/coverage/", "/vendor/", "/target/", "/.next/", "/.vite/"
        };

        var candidates = tree
            .Where(item => item.Size is > 0 and <= 200_000)
            .Where(item => !excludedSegments.Any(segment =>
                $"/{item.Path.Replace('\\', '/')}/".Contains(segment, StringComparison.OrdinalIgnoreCase)))
            .Where(item => IsAnalyzableFile(item.Path))
            .Select(item => new
            {
                Item = item,
                Score = ScoreFile(item.Path)
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Item.Path.Count(character => character == '/'))
            .ThenBy(item => item.Item.Path)
            .Take(80)
            .Select(item => item.Item)
            .ToList();

        return candidates;
    }

    private static bool IsAnalyzableFile(string path)
    {
        var fileName = Path.GetFileName(path);
        var extension = Path.GetExtension(path);
        var exactNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "README.md", "package.json", "pnpm-lock.yaml", "yarn.lock", "package-lock.json",
            "Dockerfile", "docker-compose.yml", "compose.yml", ".env.example",
            "Program.cs", "Startup.cs", "appsettings.json", "tsconfig.json", "vite.config.ts",
            "vite.config.js", "next.config.js", "next.config.ts", "tailwind.config.js",
            "tailwind.config.ts", "pom.xml", "build.gradle", "requirements.txt", "pyproject.toml"
        };
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".csproj", ".ts", ".tsx", ".js", ".jsx", ".json", ".md",
            ".sql", ".yml", ".yaml", ".java", ".py", ".go", ".rs", ".php"
        };

        return exactNames.Contains(fileName) || extensions.Contains(extension);
    }

    private static int ScoreFile(string path)
    {
        var normalized = path.Replace('\\', '/');
        var fileName = Path.GetFileName(normalized);
        var score = 0;

        if (fileName.Contains("README", StringComparison.OrdinalIgnoreCase)) score += 100;
        if (fileName is "package.json" or "Program.cs" or "Startup.cs" or "Dockerfile" or "docker-compose.yml" or "compose.yml") score += 90;
        if (fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || fileName is "pom.xml" or "build.gradle") score += 80;
        if (normalized.Contains("/Controllers/", StringComparison.OrdinalIgnoreCase)) score += 70;
        if (normalized.Contains("/Services/", StringComparison.OrdinalIgnoreCase)) score += 65;
        if (normalized.Contains("/Models/", StringComparison.OrdinalIgnoreCase)) score += 55;
        if (normalized.Contains("/src/", StringComparison.OrdinalIgnoreCase)) score += 45;
        if (normalized.Contains("test", StringComparison.OrdinalIgnoreCase)) score += 40;
        if (fileName.Contains("config", StringComparison.OrdinalIgnoreCase)) score += 35;
        if (Path.GetExtension(fileName) is ".ts" or ".tsx" or ".cs" or ".java" or ".py") score += 30;
        if (Path.GetExtension(fileName) is ".json" or ".yml" or ".yaml") score += 15;

        return score;
    }

    private static bool TryParseGithubRepo(string repoUrl, out string owner, out string repo)
    {
        owner = string.Empty;
        repo = string.Empty;

        if (!Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri)
            || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 2)
        {
            return false;
        }

        owner = segments[0];
        repo = segments[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? segments[1][..^4]
            : segments[1];
        return true;
    }

    private static decimal CalculateRepositoryScore(string readme, RepositorySnapshot snapshot)
    {
        var score = 20m;
        if (readme.Length >= 500) score += 20;
        if (readme.Contains("install", StringComparison.OrdinalIgnoreCase)) score += 15;
        if (readme.Contains("usage", StringComparison.OrdinalIgnoreCase)
            || readme.Contains("run", StringComparison.OrdinalIgnoreCase)) score += 15;
        if (readme.Contains("http", StringComparison.OrdinalIgnoreCase)) score += 15;
        if (readme.Contains("screenshot", StringComparison.OrdinalIgnoreCase)
            || readme.Contains("demo", StringComparison.OrdinalIgnoreCase)) score += 15;
        if (snapshot.Languages.Count > 0) score += 10;
        if (snapshot.Files.Any(file => file.Path.Contains("test", StringComparison.OrdinalIgnoreCase))) score += 10;
        if (snapshot.Files.Any(file => file.Path.Contains("Dockerfile", StringComparison.OrdinalIgnoreCase)
            || file.Path.Contains("docker-compose", StringComparison.OrdinalIgnoreCase))) score += 10;
        if (snapshot.Files.Any(file => file.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || file.Path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)
            || file.Path.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase))) score += 10;
        return Math.Min(score, 100m);
    }

    private static string TrimForPrompt(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string NormalizeAiJson(string value)
    {
        var trimmed = value.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return trimmed;
        }

        var candidate = trimmed[start..(end + 1)];
        try
        {
            using var document = JsonDocument.Parse(candidate);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
        catch (JsonException)
        {
            return trimmed;
        }
    }

    private static string BuildFilesPrompt(IReadOnlyList<RepositoryFileSnapshot> files)
    {
        var builder = new StringBuilder();
        foreach (var file in files)
        {
            builder.AppendLine($"--- FILE: {file.Path} size={file.Size} truncated={file.Truncated} ---");
            builder.AppendLine(file.Content);
            builder.AppendLine();
        }

        return TrimForPrompt(builder.ToString(), 140000);
    }

    private static string BuildCommitHistoryPrompt(IReadOnlyList<RepositoryCommitSnapshot> commits)
    {
        if (commits.Count == 0)
        {
            return "No commit history was available from GitHub API.";
        }

        var builder = new StringBuilder();
        foreach (var commit in commits)
        {
            builder.Append("- ");
            builder.Append(commit.Sha);
            builder.Append(" | ");
            builder.Append(commit.Date?.ToString("O") ?? "unknown-date");
            builder.Append(" | ");
            builder.Append(commit.AuthorLogin ?? "unknown-author");
            builder.Append(" | ");
            builder.AppendLine(commit.Message);
        }

        return TrimForPrompt(builder.ToString(), 6000);
    }

    private static GithubRepositoryResponse ToResponse(GithubRepository repository) =>
        new(
            repository.Id,
            repository.RepoName,
            repository.RepoUrl,
            repository.Description,
            repository.MainLanguage,
            repository.AiSummary,
            repository.TechStackJson,
            repository.QualityScore,
            repository.LastSyncedAt,
            repository.CreatedAt,
            repository.UpdatedAt);

    private sealed class GithubRepoDto
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("language")]
        public string? Language { get; init; }
    }

    private sealed class GithubOAuthTokenDto
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }

        [JsonPropertyName("scope")]
        public string? Scope { get; init; }
    }

    private sealed class GithubUserProfileDto
    {
        [JsonPropertyName("id")]
        public long? Id { get; init; }

        [JsonPropertyName("login")]
        public string Login { get; init; } = string.Empty;
    }

    private sealed class GithubRepositoryMetadataDto
    {
        [JsonPropertyName("default_branch")]
        public string? DefaultBranch { get; init; }
    }

    private sealed class GithubTreeResponseDto
    {
        [JsonPropertyName("tree")]
        public List<GithubTreeItemDto>? Tree { get; init; }
    }

    private sealed class GithubTreeItemDto
    {
        [JsonPropertyName("path")]
        public string Path { get; init; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;

        [JsonPropertyName("sha")]
        public string? Sha { get; init; }

        [JsonPropertyName("size")]
        public long? Size { get; init; }
    }

    private sealed class GithubBlobDto
    {
        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;

        [JsonPropertyName("encoding")]
        public string Encoding { get; init; } = string.Empty;
    }

    private sealed class GithubCommitDto
    {
        [JsonPropertyName("sha")]
        public string Sha { get; init; } = string.Empty;

        [JsonPropertyName("commit")]
        public GithubCommitDetailDto? Commit { get; init; }

        [JsonPropertyName("author")]
        public GithubUserDto? Author { get; init; }
    }

    private sealed class GithubCommitDetailDto
    {
        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("author")]
        public GithubCommitAuthorDto? Author { get; init; }
    }

    private sealed class GithubCommitAuthorDto
    {
        [JsonPropertyName("date")]
        public DateTimeOffset? Date { get; init; }
    }

    private sealed class GithubUserDto
    {
        [JsonPropertyName("login")]
        public string? Login { get; init; }
    }

    private sealed record RepositorySnapshot(
        string DefaultBranch,
        IReadOnlyDictionary<string, long> Languages,
        IReadOnlyList<RepositoryCommitSnapshot> RecentCommits,
        IReadOnlyList<string> TreeSample,
        IReadOnlyList<RepositoryFileSnapshot> Files,
        int TotalFiles);

    private sealed record RepositoryCommitSnapshot(
        string Sha,
        string Message,
        DateTimeOffset? Date,
        string? AuthorLogin);

    private sealed record RepositoryFileSnapshot(
        string Path,
        long? Size,
        string Content,
        bool Truncated);
}

public sealed record GithubOAuthLoginRequest(string? ReturnUrl, string? Scope);

public sealed record GithubOAuthLoginResponse(string AuthorizationUrl, string State, string ReturnUrl);

public sealed record GithubSyncRequest(string? Username, bool? IncludePrivate);

public sealed record AnalyzeReadmeRequest(Guid? RepositoryId, string? RepoUrl, string? ReadmeContent);

public sealed record GithubRepositoryResponse(
    Guid Id,
    string RepoName,
    string RepoUrl,
    string? Description,
    string? MainLanguage,
    string? AiSummary,
    string? TechStackJson,
    decimal? QualityScore,
    DateTimeOffset? LastSyncedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

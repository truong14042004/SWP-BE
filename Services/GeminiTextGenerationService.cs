using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SWP_BE.Options;

namespace SWP_BE.Services;

public sealed class GeminiTextGenerationService(
    HttpClient httpClient,
    IOptions<AiOptions> options) : IAiTextGenerationService
{
    private readonly AiOptions _options = options.Value;

    public async Task<AiTextResult> GenerateAsync(
        string systemInstruction,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        if (!_options.Provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only Gemini AI provider is configured for this backend.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured. Set AI:ApiKey or AI__ApiKey.");
        }

        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-1.5-flash" : _options.Model.Trim();
        var request = new GeminiGenerateRequest(
            new GeminiContent([new GeminiPart(systemInstruction)]),
            [
                new GeminiContent([new GeminiPart(userPrompt)])
            ],
            new GeminiGenerationConfig(0.25m, 16384));

        var response = await httpClient.PostAsJsonAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(_options.ApiKey)}",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Gemini request failed: {(int)response.StatusCode} {detail}");
        }

        var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken);
        var text = result?.Candidates?
            .SelectMany(candidate => candidate.Content?.Parts ?? [])
            .Select(part => part.Text)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Gemini returned an empty response.");
        }

        return new AiTextResult(text.Trim(), model, result?.UsageMetadata?.TotalTokenCount);
    }

    private sealed record GeminiGenerateRequest(
        [property: JsonPropertyName("systemInstruction")] GeminiContent SystemInstruction,
        [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("temperature")] decimal Temperature,
        [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens);

    private sealed record GeminiContent(
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string Text);

    private sealed record GeminiGenerateResponse(
        [property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate>? Candidates,
        [property: JsonPropertyName("usageMetadata")] GeminiUsageMetadata? UsageMetadata);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content);

    private sealed record GeminiUsageMetadata(
        [property: JsonPropertyName("totalTokenCount")] int? TotalTokenCount);
}

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

    public Task<AiTextResult> GenerateAsync(
        string systemInstruction,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        return GenerateAsync(systemInstruction, userPrompt, asJson: false, cancellationToken);
    }

    public async Task<AiTextResult> GenerateAsync(
        string systemInstruction,
        string userPrompt,
        bool asJson,
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

        var primaryModel = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.1-flash-lite" : _options.Model.Trim();

        // 3-tier fallback chain — try newer/cheaper first, settle on most-available 1.5-flash if all else fails.
        var modelsToTry = new List<string> { primaryModel };
        foreach (var fallback in new[] { "gemini-3.1-flash-lite", "gemini-2.5-flash-lite", "gemini-1.5-flash" })
        {
            if (!modelsToTry.Any(item => item.Equals(fallback, StringComparison.OrdinalIgnoreCase)))
            {
                modelsToTry.Add(fallback);
            }
        }

        Exception? lastError = null;
        foreach (var model in modelsToTry)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    return await CallGeminiAsync(model, systemInstruction, userPrompt, asJson, cancellationToken);
                }
                catch (TransientGeminiException ex) when (attempt < 2)
                {
                    lastError = ex;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 1s, 2s
                    await Task.Delay(delay, cancellationToken);
                }
                catch (TransientGeminiException ex)
                {
                    lastError = ex;
                    break; // exhausted retries for this model, try next model
                }
            }
        }

        throw lastError ?? new InvalidOperationException("Gemini request failed after retries.");
    }

    private async Task<AiTextResult> CallGeminiAsync(
        string model,
        string systemInstruction,
        string userPrompt,
        bool asJson,
        CancellationToken cancellationToken)
    {
        // For 2.x / 3.x models, disable thinking to avoid empty responses caused by thinking-budget eating tokens.
        var disableThinking = model.Contains("3.", StringComparison.OrdinalIgnoreCase)
            || model.Contains("2.5", StringComparison.OrdinalIgnoreCase)
            || model.Contains("2.0", StringComparison.OrdinalIgnoreCase);

        var generationConfig = new GeminiGenerationConfig(
            Temperature: 0.25m,
            MaxOutputTokens: 32768,
            ResponseMimeType: asJson ? "application/json" : null,
            ThinkingConfig: disableThinking ? new GeminiThinkingConfig(0) : null);

        var request = new GeminiGenerateRequest(
            new GeminiContent([new GeminiPart(systemInstruction)]),
            [
                new GeminiContent([new GeminiPart(userPrompt)])
            ],
            generationConfig);

        var response = await httpClient.PostAsJsonAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(_options.ApiKey)}",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            var statusCode = (int)response.StatusCode;

            // 429, 500, 502, 503, 504 are retryable
            if (statusCode is 429 or 500 or 502 or 503 or 504)
            {
                throw new TransientGeminiException(
                    $"Gemini transient error: {statusCode} {detail}");
            }

            throw new InvalidOperationException($"Gemini request failed: {statusCode} {detail}");
        }

        var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken);
        var text = result?.Candidates?
            .SelectMany(candidate => candidate.Content?.Parts ?? [])
            .Select(part => part.Text)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.IsNullOrWhiteSpace(text))
        {
            var finishReason = result?.Candidates?.FirstOrDefault()?.FinishReason ?? "Unknown";
            throw new InvalidOperationException(
                $"Gemini returned an empty response (finishReason={finishReason}). " +
                $"If using a 2.5 model, this is usually caused by thinking budget exhausting the output tokens.");
        }

        return new AiTextResult(text.Trim(), model, result?.UsageMetadata?.TotalTokenCount);
    }

    private sealed class TransientGeminiException(string message) : Exception(message);


    private sealed record GeminiGenerateRequest(
        [property: JsonPropertyName("systemInstruction")] GeminiContent SystemInstruction,
        [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("temperature")] decimal Temperature,
        [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens,
        [property: JsonPropertyName("responseMimeType"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ResponseMimeType,
        [property: JsonPropertyName("thinkingConfig"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] GeminiThinkingConfig? ThinkingConfig);

    private sealed record GeminiThinkingConfig(
        [property: JsonPropertyName("thinkingBudget")] int ThinkingBudget);

    private sealed record GeminiContent(
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string Text);

    private sealed record GeminiGenerateResponse(
        [property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate>? Candidates,
        [property: JsonPropertyName("usageMetadata")] GeminiUsageMetadata? UsageMetadata);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content,
        [property: JsonPropertyName("finishReason")] string? FinishReason);

    private sealed record GeminiUsageMetadata(
        [property: JsonPropertyName("totalTokenCount")] int? TotalTokenCount);
}

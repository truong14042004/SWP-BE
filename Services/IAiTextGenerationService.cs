namespace SWP_BE.Services;

public interface IAiTextGenerationService
{
    Task<AiTextResult> GenerateAsync(
        string systemInstruction,
        string userPrompt,
        CancellationToken cancellationToken);
}

public sealed record AiTextResult(
    string Text,
    string Model,
    int? TokensUsed);

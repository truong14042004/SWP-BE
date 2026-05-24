namespace SWP_BE.Services;

public interface ISkillExtractor
{
    IReadOnlyList<ExtractedKeyword> Extract(string text);
}

public sealed record ExtractedKeyword(string Keyword, int Count);

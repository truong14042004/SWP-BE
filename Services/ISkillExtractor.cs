namespace SWP_BE.Services;

public interface ISkillExtractor
{
    IReadOnlyList<ExtractedKeyword> Extract(string text);

    /// <summary>
    /// Returns true if a posting looks like an IT job, based on its title and a
    /// signal text (e.g. the JSON-LD "skills" field, or the description for
    /// existing rows). Used to filter out the non-IT postings that TopDev's
    /// all-industry sitemap mixes in. Errs toward precision: when neither the
    /// signal text names a known IT skill nor the title carries an IT role
    /// keyword, the posting is treated as non-IT.
    /// </summary>
    bool LooksLikeItJob(string? title, string? signalText);
}

public sealed record ExtractedKeyword(string Keyword, int Count);

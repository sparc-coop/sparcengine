using Sparc.Blossom.Authentication;
using System.Globalization;

namespace Sparc.Engine;

public class TovikTranslator(IEnumerable<ITranslator> translators, IRepository<TextContent> content)
{
    internal static List<Language>? Languages;

    internal IEnumerable<ITranslator> Translators { get; } = translators;
    public IRepository<TextContent> Content { get; } = content;

    public async Task<List<Language>> GetLanguagesAsync()
    {
        if (Languages == null)
        {
            Languages = [];
            foreach (var translator in Translators.OrderBy(x => x.Priority))
            {
                var languages = await translator.GetLanguagesAsync();
                Languages.AddRange(languages.Where(x => !Languages.Any(y => y.Matches(x))));
            }
        }

        Languages = Languages.OrderBy(x => x.DisplayName)
            .ThenBy(x => x.DialectId == null ? 1 : 0)
            .ToList();

        return Languages;
    }

    async Task<Language?> GetLanguageAsync(string language)
    {
        var languages = await GetLanguagesAsync();
        return languages.FirstOrDefault(x => x.Id == language);
    }

    public async Task<TextContent?> TranslateAsync(TextContent message, Language toLanguage, string? additionalContext = null)
        => (await TranslateAsync([message], [toLanguage], additionalContext)).FirstOrDefault();

    public async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, List<Language> toLanguages, string? additionalContext = null)
    {
        var translatedMessages = new List<TextContent>();

        foreach (var toLanguage in toLanguages)
        {
            var translators = messages.GroupBy(x => GetBestTranslator(x.Language, toLanguage));
            foreach (var messageList in translators)
            {
                var result = await messageList.Key.TranslateAsync(messageList.ToList(), [toLanguage], additionalContext);
                foreach (var message in result)
                    translatedMessages.Add(message);
            }
        }
        
        return translatedMessages.ToList();
    }

    internal async Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage)
    {
        if (fromLanguage == toLanguage)
            return text;

        var language = await GetLanguageAsync(toLanguage)
            ?? throw new ArgumentException($"Language {toLanguage} not found");

        var from = await GetLanguageAsync(fromLanguage);
        var message = new TextContent("", from!, text);
        var result = await TranslateAsync([message], [language]);
        return result?.FirstOrDefault()?.Text;
    }

    internal ITranslator GetBestTranslator(Language fromLanguage, Language toLanguage)
    {
        return Translators
            .OrderBy(x => x.Priority)
            .FirstOrDefault(x => x.CanTranslate(fromLanguage, toLanguage))
            ?? throw new Exception($"No translator found for {fromLanguage.Id} to {toLanguage.Id}");
    }

    internal static Language? GetLanguage(string? languageClaim)
    {
        if (Languages == null || string.IsNullOrWhiteSpace(languageClaim))
            return null;

        var languages = languageClaim
            .Split(',')
            .Select(l => l.Split(';')[0].Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (languages.Count == 0)
            return null;

        // Try to find a matching language in LanguagesSpoken or create a new one
        foreach (var langCode in languages)
        {
            // Try to match by Id or DialectId
            var match = Languages
                .OrderBy(x => x.DialectId != null ? 0 : 1)
                .FirstOrDefault(l => l.Matches(langCode));

            if (match != null)
                return match;
        }

        return null;
    }

    internal static BlossomRegion? GetLocale(string languageClaim)
    {
        var languages = languageClaim
            .Split(',')
            .Select(l => l.Split(';')[0].Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (languages.Count == 0)
            return null;

        try
        {
            var locale = languages
                .Where(x => x.Contains('-'))
                .Select(x => x.Split('-').Last())
                .Select(region => new RegionInfo(region))
                .FirstOrDefault();

            return locale == null ? null : new(locale);
        }
        catch
        {
            return null;
        }
    }

    internal static IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> items,
                                                       int maxItems)
    {
        return items.Select((item, inx) => new { item, inx })
                    .GroupBy(x => x.inx / maxItems)
                    .Select(g => g.Select(x => x.item));
    }
}
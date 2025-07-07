using Sparc.Blossom;
using Sparc.Engine;

namespace Tovik.Languages;

public class Languages(BlossomAggregateOptions<Language> options) 
    : BlossomAggregate<Language>(options)
{
    internal async Task InitializeAsync(IEnumerable<ITranslator> translators)
    {
        var languages = Repository.Query.ToList();
        if (languages.Count == 0)
        {
            languages = [];
            foreach (var translator in translators.OrderBy(x => x.Priority))
            {
                var translatorLanguages = await translator.GetLanguagesAsync();
                languages.AddRange(translatorLanguages.Where(x => !languages.Any(y => y.Matches(x))));
            }
            await Repository.AddAsync(languages);
        }
    }

    public BlossomQuery<Language> All()
        => Query().OrderBy(x => x.Id, x => x.DialectId == null ? 1 : 0);
}

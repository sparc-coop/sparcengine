using Sparc.Blossom.Data;
using Sparc.Engine;

namespace Tovik.Domains;

public class TovikDomainService(IRepository<TovikDomain> SparcDomainRepo, IRepository<TextContent> TextContentRepo)
{
    // repositories
    public IRepository<TovikDomain> SparcDomainRepository { get; } = SparcDomainRepo;
    public IRepository<TextContent> TextContentRepository { get; } = TextContentRepo;

    public async Task<string> UpdateWordCountForDomains()
    {
        var domains = await SparcDomainRepository.Query.Where(d => d.Domain != null)
            .ToListAsync();

        foreach (var domain in domains)
        {
            string normalizedDomainName = NormalizeDomain(domain.Domain);

            var domainTexts = await TextContentRepository.Query
                .Where(t => t.Domain == normalizedDomainName && t.Id != null)
                .ToListAsync();

            if (domainTexts.Count > 0)
            {
                foreach (var text in domainTexts)
                {
                    if (text.OriginalText != null)
                    {
                        int wordCount = CountWords(text.OriginalText);
                        domain.TotalUsage += wordCount;
                    }
                }
            }

            await SparcDomainRepository.UpdateAsync(domain);
        }

        return "";
    }

    private string NormalizeDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return string.Empty;

        if (domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return domain.ToLower().Substring("https://".Length);

        return domain.ToLower();
    }

    public int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var words = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

        return words.Length;
    }

}

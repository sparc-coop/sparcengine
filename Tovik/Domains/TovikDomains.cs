using Sparc.Blossom.Data;
using Sparc.Blossom.Authentication;
using Sparc.Engine;

namespace Tovik.Domains;

public class TovikDomains(BlossomAggregateOptions<SparcDomain> options, IRepository<TextContent> content) 
    : BlossomAggregate<SparcDomain>(options)
{
   public async Task<List<SparcDomain>> All()
        => await Repository.Query
            .Where(x => x.Products.Any(p => p.ProductId == "Tovik" && p.UserId == User.Id()))
            .ToListAsync();


    public async Task RegisterAsync(string domainName)
    {
        var host = SparcDomain.Normalize(domainName) 
            ?? throw new ArgumentException("Invalid domain name.", nameof(domainName));
        
        var existing = await Repository.Query
            .Where(d => d.Domain == host)
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            existing = new SparcDomain(host);
            await Repository.AddAsync(existing);
        }

        var product = existing.Products.FirstOrDefault(p => p.ProductId == "Tovik");
        if (product?.UserId != null)
            throw new Exception("This domain is already registered with Tovik.");
        
        if (product == null)
            existing.Products.Add(new("Tovik") { UserId = User.Id() });
        else
            product.UserId = User.Id();

        await Repository.UpdateAsync(existing);
    }
    
    // repositories
    public async Task<string> UpdateWordCountForDomains()
    {
        var domains = await Repository.Query.Where(d => d.Domain != null)
            .ToListAsync();

        foreach (var domain in domains)
        {
            var domainTexts = await content.Query
                .Where(t => t.Domain == domain.Domain && t.Id != null)
                .ToListAsync();

            if (domainTexts.Count > 0)
            {
                foreach (var text in domainTexts)
                {
                    if (text.OriginalText != null)
                    {
                        int wordCount = CountWords(text.OriginalText);
                        domain.TovikUsage += wordCount;
                    }
                }
            }

            await Repository.UpdateAsync(domain);
        }

        return "";
    }

    

    public int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        return words.Length;
    }

}

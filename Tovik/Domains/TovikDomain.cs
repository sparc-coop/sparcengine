using Bogus;
using Sparc.Blossom;

public class TovikDomain(string domain) : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public string Domain { get; set; } = domain;
    public List<SparcProduct> Products { get; set; } = [];
    public int TotalUsage = new Random().Next(1, 100000);
    public Dictionary<string, string?> Glossary { get; set; } = new();
    //public int TotalUsage => Products.Sum(p => p.UsageMeter);

    public bool HasProduct(string policyName) => policyName == "Auth" || Products.Any(p => p.ProductId == policyName);

    public static IEnumerable<TovikDomain> Generate(int qty)
    {
        var faker = new Faker<TovikDomain>()
            .CustomInstantiator(f => new SparcDomain(
                f.Lorem.Sentence(3)
            ));

        return faker.Generate(qty);
    }
}
namespace Tovik;

public class TovikDomain(string domain) : BlossomEntity<string>(Guid.NewGuid().ToString())
{
    public string Domain { get; set; } = domain;
    public int TotalUsage = new Random().Next(1, 100000);
    public Dictionary<string, string?> Glossary { get; set; } = new();
    //public int TotalUsage => Products.Sum(p => p.UsageMeter);
}
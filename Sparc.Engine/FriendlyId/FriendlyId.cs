
namespace Sparc.Aura;

public class FriendlyId
{
    public string WordsPath { get; }
    public static List<string> UnsafeWords { get; private set; } = [];

    public FriendlyId(IWebHostEnvironment env)
    {
        WordsPath = Path.Combine(env.ContentRootPath, "FriendlyId/words_alpha.txt");
        UnsafeWords = File.ReadLines(Path.Combine(env.ContentRootPath, "FriendlyId/words_officesafe.txt"))
            .Select(x => x.ToLower())
            .ToList();
    }

    public string Create(int wordCount = 2, int numberCount = 0)
    {
        var words = Enumerable.Range(0, wordCount).Select(_ => GetRandomWord()).ToList();
        var numbers = Enumerable.Range(0, numberCount).Select(_ => new Random().Next(10)).Select(n => n.ToString()).ToList();
        
        var all = words.Concat(numbers).ToList();
        return string.Join("-", words) + string.Join("", numbers);
    }

    public static string CreateFakeWord(int letterCount = 5, int numberCount = 0)
    {
        Random r = new();
        string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
        string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
        string Name = "";
        Name += consonants[r.Next(consonants.Length)].ToUpper();
        Name += vowels[r.Next(vowels.Length)];
        int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
        while (b < letterCount)
        {
            Name += consonants[r.Next(consonants.Length)];
            b++;
            Name += vowels[r.Next(vowels.Length)];
            b++;
        }

        if (numberCount > 0)
        {
            for (int i = 0; i < numberCount; i++)
            {
                Name += r.Next(10).ToString();
            }
        }

        return Name;
    }

    string GetRandomWord()
    {
        var random = new Random();
        var word = File.ReadLines(WordsPath)
            .Skip(random.Next(370000))
            .First()
            .Trim()
            .ToLower();

        // Check against office-unsafe words
        if (UnsafeWords.Any(x => x == word))
            return GetRandomWord();

        return word;
    }
}

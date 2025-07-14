using System.Text.Json;

namespace Sparc.Engine.Billing;

public class ExchangeRates(IConfiguration config)
{
    private readonly string _apiKey = config.GetConnectionString("ExchangeRates")
      ?? throw new InvalidOperationException(
             "ExchangeRates connection string is missing in configuration.");

    public Dictionary<string, decimal> Rates = [];
    public DateTime? LastUpdated { get; private set; }
    public DateTime? AsOfDate { get; private set; }
    public bool IsOutOfDate => Rates.Count == 0 || LastUpdated == null || LastUpdated < DateTime.UtcNow.AddHours(-24);

    public async Task<decimal> ConvertAsync(decimal amount, string from, string to, bool round = false)
    {
        from = from.ToUpper();
        to = to.ToUpper();

        if (IsOutOfDate)
            await RefreshAsync();

        if (from == to)
            return amount;

        var convertedAmount =
            from == "USD" ? amount * Rates[to]
            : to == "USD" ? amount / Rates[from]
            : amount * Rates[to] / Rates[from];

        if (round)
        {
            // Find the largest power of 10 less than or equal to 10% of the convertedAmount
            var tenPercent = convertedAmount * 0.1m;
            var magnitude = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)tenPercent)));
            // Round down to nearest 'magnitude'
            convertedAmount = Math.Floor(convertedAmount / magnitude) * magnitude;
        }

        return convertedAmount;
    }

    public async Task<List<decimal>> ConvertToNiceAmountsAsync(string toCurrency, params decimal[] usdAmounts)
    {
        var result = new List<decimal>();
        var baseAmount = NiceRound(await ConvertAsync(usdAmounts.First(), "USD", toCurrency));
        return usdAmounts.Select(x => baseAmount * x).ToList();
    }

    async Task RefreshAsync()
    {
        using var client = new HttpClient()
        {
            BaseAddress = new Uri("https://api.apilayer.com/exchangerates_data/latest")
        };

        client.DefaultRequestHeaders.Add("apikey", _apiKey);

        var response = await client.GetFromJsonAsync<ExchangeRatesResponse>("?base=USD");
        if (response?.Success == true)
        {
            Rates = response.Rates;
            AsOfDate = response.Date;
            LastUpdated = DateTime.UtcNow;
            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsync(stream, response);
            stream.Position = 0;
        }

    }

    public static int NiceRound(decimal value)
    {
        int roundedValue = (int)Math.Round(value, 0);
        var strVal = roundedValue.ToString();
        var niceStrVal = strVal[0] + new string(strVal.Skip(1).Select(x => '0').ToArray());
        return int.Parse(niceStrVal);
    }

    record ExchangeRatesResponse(string Base, DateTime Date, Dictionary<string, decimal> Rates, bool Success, long Timestamp);
}

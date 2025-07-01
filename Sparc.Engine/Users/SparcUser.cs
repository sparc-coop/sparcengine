using Sparc.Blossom.Authentication;

namespace Sparc.Engine;

public class SparcUser : BlossomUser
{
    public List<ProductKey> Products { get; set; } = [];

    public async Task<Language?> GetLanguage(HttpRequest request, SparcAuthenticator<SparcUser> auth)
    {
        if (Avatar.Language == null && !string.IsNullOrWhiteSpace(request.Headers.AcceptLanguage))
        {
            var newLanguage = TovikTranslator.GetLanguage(request.Headers.AcceptLanguage!);
            if (newLanguage != null)
                ChangeLanguage(newLanguage);

            var newLocale = TovikTranslator.GetLocale(request.Headers.AcceptLanguage!);
            if (newLocale != null)
                Avatar.Locale = newLocale;

            if (newLanguage != null || newLocale != null)
                await auth.LoginAsync(ToPrincipal());
        }

        return Avatar.Language == null ? null : TovikTranslator.GetLanguage(Avatar.Language!.Id);
    }

    public bool HasProduct(string productName)
    {
        return Products.Any(x => x.ProductId.Equals(productName, StringComparison.OrdinalIgnoreCase));
    }

    public void AddProduct(string productName)
    {
        if (HasProduct(productName))
            return;

        var serial = Guid.NewGuid().ToString();
        Products.Add(new ProductKey(productName, serial, DateTime.UtcNow, Id));
    }
}

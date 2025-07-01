using Microsoft.AspNetCore.Authentication;
using Sparc.Blossom.Data;

namespace Sparc.Engine;

public static class ContentServiceCollectionExtensions
{
    public static WebApplicationBuilder AddSparcEngineTranslation(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddScoped<ITranslator, AzureTranslator>()
            .AddScoped<ITranslator, DeepLTranslator>()
            .AddScoped<TovikTranslator>()
            .AddScoped<BlossomAggregateOptions<TextContent>>()
            .AddScoped<BlossomAggregate<TextContent>>()
            .AddScoped<Contents>();

        return builder;
    }

    public static WebApplication UseSparcEngineTranslation(this WebApplication app)
    {
        var translator = app.MapGroup("/translate");
        translator.MapGet("languages", async (TovikTranslator translator) => await translator.GetLanguagesAsync());
        //translator.MapGet("translate", async (BlossomTranslator translator, string text, string toLanguage) => await translator.TranslateAsync(text, toLanguage));
        //translator.MapGet("detect", async (BlossomTranslator translator, string text) => await translator.DetectLanguageAsync(text));
        
        return app;
    }
}

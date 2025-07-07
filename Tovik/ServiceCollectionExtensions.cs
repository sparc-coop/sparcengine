using Sparc.Blossom;

namespace Sparc.Engine;

public static class ContentServiceCollectionExtensions
{
    public static WebApplicationBuilder AddTovikTranslator(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddScoped<ITranslator, AzureTranslator>()
            .AddScoped<ITranslator, DeepLTranslator>()
            .AddScoped<TovikTranslator>()
            .AddScoped<BlossomAggregateOptions<TextContent>>()
            .AddScoped<BlossomAggregate<TextContent>>();
        return builder;
    }

    public static WebApplication UseTovikTranslator(this WebApplication app)
    {
        var translator = app.MapGroup("/tovik");
        translator.MapGet("languages", async (TovikTranslator translator) => await translator.GetLanguagesAsync());
        
        return app;
    }
}

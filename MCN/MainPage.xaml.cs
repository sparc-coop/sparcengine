using MCN;
using MCN.Components.Layout;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace MCN;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        var myAppAssembly = typeof(MainPage).Assembly;

        var closed = typeof(BlossomApp<,>).MakeGenericType(
            typeof(MauiProgram),
            typeof(MainLayout)
        );

        blazorWebView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = closed,
            Parameters = new Dictionary<string, object?>
            {
                ["ExtraAssemblies"] = new[] { myAppAssembly }
            }
        });
    }
}
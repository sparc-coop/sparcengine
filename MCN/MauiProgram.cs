using MCN.Components.Layout;
using Microsoft.Extensions.Logging;
using Sparc.Blossom;

namespace MCN
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp(string[]? args = null)
        {
            //var builder = MauiApp.CreateBuilder();
            //builder
            //    .UseMauiApp<App>()
            //    .ConfigureFonts(fonts =>
            //    {
            //        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            //    });

            //builder.Services.AddMauiBlazorWebView();

            Sparc.Blossom.MauiProgram.LayoutType = typeof(MainLayout);
#if ANDROID
            var builder = BlossomApplication.CreateBuilder<App>(args);

            var blossomApp = builder.Build();
            var androidApp = (Sparc.Blossom.Platforms.Android.BlossomAndroidApplication)blossomApp;
            return Sparc.Blossom.Platforms.Android.BlossomAndroidApplication.MauiApp;
            //#elif WINDOWS
            //            var builder = BlossomApplication.CreateBuilder<App>(args);

            //            var blossomApp = builder.Build();
            //            var windowsApp = (Sparc.Blossom.Platforms.Windows.BlossomWindowsApplication)blossomApp;
            //            return Sparc.Blossom.Platforms.Windows.BlossomWindowsApplication.MauiApp;
#elif IOS
            var builder = BlossomApplication.CreateBuilder(args);

            var blossomApp = builder.Build();
            var iosApp = (Sparc.Blossom.Platforms.iOS.BlossomiOSApplication)blossomApp;
            return Sparc.Blossom.Platforms.iOS.BlossomiOSApplication.MauiApp;
#else

            var builder = MauiApp.CreateBuilder();

            return builder.Build();
#endif

            //#if DEBUG
            //            builder.Services.AddBlazorWebViewDeveloperTools();
            //    		builder.Logging.AddDebug();
            //#endif

            //return builder.Build();
        }
    }
}

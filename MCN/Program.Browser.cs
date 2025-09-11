#if BROWSER
using MCN;
using MCN.Components;
using Microsoft.AspNetCore.Components.Routing;
using Sparc.Blossom;

await BlossomApplication
    .CreateBuilder<BlazorApp> (args)
    .Build()
    .RunAsync<BlazorApp>();
#endif
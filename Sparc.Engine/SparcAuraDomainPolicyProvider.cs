using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Net.Http.Headers;

namespace Sparc.Aura;

public class SparcAuraDomainPolicyProvider : ICorsPolicyProvider
{
    static CorsPolicy AllowAll = new CorsPolicyBuilder()
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
        .Build();

    static readonly Dictionary<string, CorsPolicy> _policies = [];

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        if (policyName == null)
            return AllowAll;

        var currentDomain = context.Request.Headers.Origin.ToString();
        if (_policies.TryGetValue(currentDomain, out var existingPolicy))
            return existingPolicy;

        var newPolicy = new CorsPolicyBuilder()
            .WithOrigins(currentDomain)
            .WithMethods("GET", "POST")
            .WithHeaders(HeaderNames.ContentType, HeaderNames.AcceptLanguage)
            .AllowCredentials();
       
        _policies.TryAdd(currentDomain, newPolicy.Build());
        return  _policies[currentDomain];
    }
}

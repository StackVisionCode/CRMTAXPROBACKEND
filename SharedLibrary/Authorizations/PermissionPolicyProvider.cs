using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SharedLibrary.Authorizations;

public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) =>
        _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string name)
    {
        if (!HasPermissionAttribute.TryParse(name, out var code))
            return _fallback.GetPolicyAsync(name);

        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(code))
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

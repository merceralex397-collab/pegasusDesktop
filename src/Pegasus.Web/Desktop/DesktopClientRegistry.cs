using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Abstractions;

namespace Pegasus.Web.Desktop;

/// <summary>
/// Owns the single public OpenIddict client used by the native desktop.
/// Reconciliation is idempotent and preserves an administrator-disabled
/// registration by using the password grant permission as its enabled bit.
/// </summary>
public sealed class DesktopClientRegistry(
    IOpenIddictApplicationManager applications,
    IMemoryCache cache)
{
    private static readonly TimeSpan EnsureLifetime = TimeSpan.FromHours(24);
    private const string EnabledCachePrefix = "pegasus-desktop:enabled:";
    private const string EnsuredCachePrefix = "pegasus-desktop:ensured:";

    private static string EnabledCacheKey => EnabledCachePrefix + DesktopSession.ClientId;
    private static string EnsuredCacheKey => EnsuredCachePrefix + DesktopSession.ClientId;

    public async Task EnsureRegisteredAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(EnsuredCacheKey, out _))
        {
            return;
        }

        var application = await applications.FindByClientIdAsync(
            DesktopSession.ClientId,
            cancellationToken);
        if (application is null)
        {
            await applications.CreateAsync(
                CanonicalDescriptor(enabled: true),
                cancellationToken);
        }
        else
        {
            var enabled = await applications.HasPermissionAsync(
                application,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                cancellationToken);
            await applications.UpdateAsync(
                application,
                CanonicalDescriptor(enabled),
                cancellationToken);
        }

        cache.Set(EnsuredCacheKey, value: true, EnsureLifetime);
        cache.Remove(EnabledCacheKey);
    }

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<bool>(EnabledCacheKey, out var cached))
        {
            return cached;
        }

        var application = await applications.FindByClientIdAsync(
            DesktopSession.ClientId,
            cancellationToken);
        var enabled = application is not null
            && await applications.HasPermissionAsync(
                application,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                cancellationToken);
        cache.Set(EnabledCacheKey, enabled, TimeSpan.FromSeconds(5));
        return enabled;
    }

    private static OpenIddictApplicationDescriptor CanonicalDescriptor(bool enabled)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = DesktopSession.ClientId,
            DisplayName = DesktopSession.ClientDisplayName,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit
        };
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.Prefixes.Scope + DesktopSession.Scope);
        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess);
        if (enabled)
        {
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
        }

        return descriptor;
    }
}

using Microsoft.Extensions.Configuration;

namespace Pegasus.Web.Api;

/// <summary>
/// Fixed names for the configuration-gated native desktop API surface.
/// </summary>
public static class DesktopGateway
{
    public const string FeatureFlag = "Features:DesktopGateway";
    public const string BasePath = "/api/v1";
    public const string AuthorizationPolicy = "DesktopApi";
    public const string ActorItemKey = "Pegasus.DesktopGateway.Actor";
}

/// <summary>
/// Composition-time options for the native desktop API surface.
/// </summary>
public sealed record DesktopGatewayOptions
{
    /// <summary>
    /// Returns no options when the feature flag is absent or false, leaving the
    /// route group and its services uncomposed.
    /// </summary>
    public static DesktopGatewayOptions? TryCreate(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.GetValue<bool>(DesktopGateway.FeatureFlag)
            ? new DesktopGatewayOptions()
            : null;
    }
}

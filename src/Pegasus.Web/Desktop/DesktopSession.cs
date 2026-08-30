using Pegasus.Web.Mcp;

namespace Pegasus.Web.Desktop;

/// <summary>
/// Fixed names and lifetimes for the first-party Pegasus Desktop session.
/// </summary>
public static class DesktopSession
{
    public const string ClientId = "pegasus-desktop";
    public const string ClientDisplayName = "Pegasus Desktop";
    public const string Scope = "pegasus.desktop";
    public const string TokenEndpointPath = AutomationMcp.TokenEndpointPath;
    public const string OriginalIssueClaim = "pegasus:original-issued-at";
    public const string SecurityStampClaim = "pegasus:security-stamp";
    public const string CertificateSubject = "CN=Collision Engineers";

    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan RefreshTokenLifetime = Pegasus.Core.Actors.StaffSessionPolicy.IdleLifetime;
    public static readonly TimeSpan AbsoluteSessionLifetime = Pegasus.Core.Actors.StaffSessionPolicy.AbsoluteLifetime;
}

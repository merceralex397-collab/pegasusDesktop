using System.Net;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Core.Actors;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Pegasus.Core.Identity;
using Pegasus.Infrastructure.Persistence;
using Pegasus.Web.Api;
using Pegasus.Web.Desktop;
using Pegasus.Web.Mcp;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Pegasus.IntegrationTests;

[Trait("Category", "SqlServer")]
public sealed class DesktopTokenIssuanceTests
{
    private const string Password = "desktop-test-password";
    private static readonly DateTimeOffset StartUtc =
        new(2031, 5, 6, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task PasswordGrantIssuesClaimsAndRollingRefreshToken()
    {
        var clock = new MutableTimeProvider(StartUtc);
        using var baseFactory = new IntakeWebApplicationFactory(clock);
        using var factory = WithDesktopGateway(baseFactory);
        var user = await SeedStaffAsync(factory, enabled: true);
        using var client = CreateClient(factory);

        using var issued = await RequestPasswordAsync(client, user);
        var accessToken = issued.RootElement.GetProperty("access_token").GetString();
        var refreshToken = issued.RootElement.GetProperty("refresh_token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
        Assert.Equal(600, issued.RootElement.GetProperty("expires_in").GetInt32());

        using var claimsRequest = CreateClaimsRequest(accessToken!);
        using var claimsResponse = await client.SendAsync(claimsRequest);
        var claimsBody = await claimsResponse.Content.ReadAsStringAsync();
        Assert.True(claimsResponse.IsSuccessStatusCode, claimsBody);
        using var claimsDocument = JsonDocument.Parse(claimsBody);
        var principal = claimsDocument.RootElement;
        Assert.Equal(
            user.Id.ToString("D"),
            principal.GetProperty(Claims.Subject).EnumerateArray().Single().GetString());
        Assert.Equal(
            [StaffRoleNames.Engineer],
            principal.GetProperty(Claims.Role).EnumerateArray()
                .Select(claim => claim.GetString()));
        Assert.Equal(
            StartUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            principal.GetProperty(DesktopSession.OriginalIssueClaim)
                .EnumerateArray().Single().GetString());

        using var refreshed = await RequestRefreshAsync(client, refreshToken!);
        var refreshedToken = refreshed.RootElement.GetProperty("refresh_token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(refreshedToken));
        Assert.NotEqual(refreshToken, refreshedToken);
    }

    [Fact]
    public async Task RefreshGrantEnforcesTheEightHourAbsoluteSessionCap()
    {
        var clock = new MutableTimeProvider(StartUtc);
        using var baseFactory = new IntakeWebApplicationFactory(clock);
        using var factory = WithDesktopGateway(baseFactory);
        var user = await SeedStaffAsync(factory, enabled: true);
        using var client = CreateClient(factory);

        using var issued = await RequestPasswordAsync(client, user);
        var refreshToken = issued.RootElement.GetProperty("refresh_token").GetString()!;

        // Keep the two-hour idle token alive while retaining the original
        // issue timestamp. The eighth-hour exchange must then be rejected by
        // the Desktop handler rather than by refresh-token expiry.
        for (var hour = 1; hour < 8; hour++)
        {
            clock.Advance(TimeSpan.FromHours(1));
            using var refreshed = await RequestRefreshAsync(client, refreshToken);
            refreshToken = refreshed.RootElement.GetProperty("refresh_token").GetString()!;
        }

        clock.Advance(TimeSpan.FromHours(1));
        using var expired = await PostTokenAsync(
            client,
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = DesktopSession.ClientId,
                ["refresh_token"] = refreshToken
            });
        var body = await expired.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("invalid_grant", document.RootElement.GetProperty("error").GetString());
        Assert.Equal(
            "absolute-session-expired",
            document.RootElement.GetProperty("error_description").GetString());
    }

    [Fact]
    public async Task DisabledAccountCannotObtainDesktopToken()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        using var factory = WithDesktopGateway(baseFactory);
        var user = await SeedStaffAsync(factory, enabled: false);
        using var client = CreateClient(factory);

        using var response = await PostTokenAsync(
            client,
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = DesktopSession.ClientId,
                ["username"] = user.UserName!,
                ["password"] = Password,
                ["scope"] = $"{DesktopSession.Scope} offline_access"
            });
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("invalid_grant", document.RootElement.GetProperty("error").GetString());
        Assert.Equal(
            "account-disabled",
            document.RootElement.GetProperty("error_description").GetString());
    }

    [Fact]
    public async Task CombinedCompositionKeepsDesktopRefreshRollingAndAutomationAvailable()
    {
        var clock = new MutableTimeProvider(StartUtc);
        using var baseFactory = new IntakeWebApplicationFactory(clock);
        using var factory = WithDesktopGateway(baseFactory, automation: true);
        var user = await SeedStaffAsync(factory, enabled: true);
        using var client = CreateClient(factory);

        using var issued = await RequestPasswordAsync(client, user);
        var refreshToken = issued.RootElement.GetProperty("refresh_token").GetString()!;

        // With sliding refresh enabled, this exchange remains valid beyond
        // the original two-hour idle window. Automation's own flow is present
        // in the same composition and continues to issue its client token.
        clock.Advance(TimeSpan.FromHours(1));
        using var firstRefresh = await RequestRefreshAsync(client, refreshToken);
        refreshToken = firstRefresh.RootElement.GetProperty("refresh_token").GetString()!;
        clock.Advance(TimeSpan.FromMinutes(30));
        using var secondRefresh = await RequestRefreshAsync(client, refreshToken);
        refreshToken = secondRefresh.RootElement.GetProperty("refresh_token").GetString()!;
        clock.Advance(TimeSpan.FromHours(1));
        using var thirdRefresh = await RequestRefreshAsync(client, refreshToken);
        Assert.False(string.IsNullOrWhiteSpace(
            thirdRefresh.RootElement.GetProperty("access_token").GetString()));

        var automationToken = await AutomationMcpTestSupport.RequestTokenAsync(
            client,
            AutomationMcp.CasesScope);
        Assert.False(string.IsNullOrWhiteSpace(automationToken));
        Assert.Equal(1, await baseFactory.Database.ScalarAsync<int>(
            """
            SELECT COUNT(*) FROM OpenIddictApplications
            WHERE ClientId = N'pegasus-desktop'
              AND ClientType = N'public'
              AND (ClientSecret IS NULL OR ClientSecret = N'')
            """));
    }

    [Fact]
    public async Task CombinedCompositionRejectsDesktopRefreshAfterSecurityStampChanges()
    {
        var clock = new MutableTimeProvider(StartUtc);
        using var baseFactory = new IntakeWebApplicationFactory(clock);
        using var factory = WithDesktopGateway(baseFactory, automation: true);
        var user = await SeedStaffAsync(factory, enabled: true);
        using var client = CreateClient(factory);
        using var issued = await RequestPasswordAsync(client, user);
        var refreshToken = issued.RootElement.GetProperty("refresh_token").GetString()!;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<PegasusIdentityUser>>();
            var trackedUser = await userManager.FindByIdAsync(user.Id.ToString("D"));
            Assert.NotNull(trackedUser);
            var result = await userManager.UpdateSecurityStampAsync(trackedUser);
            Assert.True(result.Succeeded);
        }

        using var response = await PostTokenAsync(
            client,
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = DesktopSession.ClientId,
                ["refresh_token"] = refreshToken
            });
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("invalid_grant", document.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task DesktopClientCannotUseAutomationGrant()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        using var factory = WithDesktopGateway(baseFactory, automation: true);
        using var client = CreateClient(factory);

        using var response = await PostTokenAsync(
            client,
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = DesktopSession.ClientId
            });
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("invalid_request", document.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task DesktopPasswordGrantReturnsTooManyRequestsOnEleventhAttempt()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        using var factory = WithDesktopGateway(baseFactory);
        var user = await SeedStaffAsync(factory, enabled: true);
        using var client = CreateClient(factory);

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            using var response = await PostTokenAsync(
                client,
                new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["client_id"] = DesktopSession.ClientId,
                    ["username"] = user.UserName!,
                    ["password"] = "wrong-password",
                    ["scope"] = DesktopSession.Scope
                });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using var limited = await PostTokenAsync(
            client,
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = DesktopSession.ClientId,
                ["username"] = user.UserName!,
                ["password"] = "wrong-password",
                ["scope"] = DesktopSession.Scope
            });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("60", limited.Headers.RetryAfter?.ToString());
    }

    [Fact]
    public async Task DesktopPasswordGrantIsCoveredByTheGlobalSignInLimiter()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        using var factory = WithDesktopGateway(baseFactory);
        var user = await SeedStaffAsync(factory, enabled: true);
        using var client = CreateClient(factory);

        var globalLimiter = factory.Services
            .GetRequiredService<System.Threading.RateLimiting.FixedWindowRateLimiter>();
        for (var attempt = 0; attempt < StaffSessionPolicy.SignInAttemptsGlobalPerMinute - 1; attempt++)
        {
            using var lease = await globalLimiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);
        }

        // The Desktop request consumes the last global permit. Without the
        // pre-UseRateLimiter global middleware it would reach the handler and
        // return invalid_grant instead of consuming the global budget.
        using var lastPermitted = await PostTokenAsync(
            client,
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = DesktopSession.ClientId,
                ["username"] = user.UserName!,
                ["password"] = "wrong-password",
                ["scope"] = DesktopSession.Scope
            });
        Assert.Equal(HttpStatusCode.BadRequest, lastPermitted.StatusCode);

        using var limited = await PostTokenAsync(
            client,
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = DesktopSession.ClientId,
                ["username"] = user.UserName!,
                ["password"] = "wrong-password",
                ["scope"] = DesktopSession.Scope
            });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("60", limited.Headers.RetryAfter?.ToString());
    }

    private static WebApplicationFactory<Program> WithDesktopGateway(
        IntakeWebApplicationFactory baseFactory,
        bool automation = false) =>
        baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(DesktopGateway.FeatureFlag, "true");
            if (automation)
            {
                builder.UseSetting(AutomationMcp.FeatureFlag, "true");
                builder.UseSetting("AutomationMcp:ClientId", AutomationMcpTestSupport.ClientId);
                builder.UseSetting("AutomationMcp:ClientSecret", AutomationMcpTestSupport.ClientSecret);
                builder.UseSetting("AutomationMcp:PublicOrigin", "http://localhost/");
                builder.UseSetting(
                    "AutomationMcp:RedirectUris",
                    AutomationMcpTestSupport.ConnectorRedirectUri);
                builder.UseSetting("AutomationMcp:RegistrationCacheSeconds", "0");
            }
            builder.ConfigureServices(services =>
                services.AddSingleton<IStartupFilter, ValidationCaptureStartupFilter>());
        });

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });

    private static async Task<PegasusIdentityUser> SeedStaffAsync(
        WebApplicationFactory<Program> factory,
        bool enabled)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var user = new PegasusIdentityUser
        {
            Id = Guid.NewGuid(),
            UserName = $"desktop-token-{Guid.NewGuid():N}",
            IsEnabled = enabled,
            MustChangePassword = false,
            LockoutEnabled = false,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<PegasusIdentityUser>>();
        var result = await userManager.CreateAsync(user, Password);
        Assert.True(
            result.Succeeded,
            string.Join(", ", result.Errors.Select(error => error.Description)));
        result = await userManager.AddToRoleAsync(user, StaffRoleNames.Engineer);
        Assert.True(
            result.Succeeded,
            string.Join(", ", result.Errors.Select(error => error.Description)));
        return user;
    }

    private static async Task<JsonDocument> RequestPasswordAsync(
        HttpClient client,
        PegasusIdentityUser user)
    {
        using var response = await PostTokenAsync(
            client,
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = DesktopSession.ClientId,
                ["username"] = user.UserName!,
                ["password"] = Password,
                ["scope"] = $"{DesktopSession.Scope} offline_access"
            });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body);
    }

    private static async Task<JsonDocument> RequestRefreshAsync(
        HttpClient client,
        string refreshToken)
    {
        using var response = await PostTokenAsync(
            client,
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = DesktopSession.ClientId,
                ["refresh_token"] = refreshToken
            });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body);
    }

    private static Task<HttpResponseMessage> PostTokenAsync(
        HttpClient client,
        Dictionary<string, string> values) =>
        client.PostAsync(DesktopSession.TokenEndpointPath, new FormUrlEncodedContent(values));

    private static HttpRequestMessage CreateClaimsRequest(string accessToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/__desktop_test/token-claims");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private sealed class ValidationCaptureStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Use(async (context, downstream) =>
            {
                if (!context.Request.Path.Equals(
                        "/__desktop_test/token-claims",
                        StringComparison.Ordinal))
                {
                    await downstream(context);
                    return;
                }

                var handler = await context.RequestServices
                    .GetRequiredService<IAuthenticationHandlerProvider>()
                    .GetHandlerAsync(
                        context,
                        OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                if (handler is IAuthenticationRequestHandler requestHandler
                    && await requestHandler.HandleRequestAsync())
                {
                    return;
                }

                var authentication = await context.AuthenticateAsync(
                    OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                if (!authentication.Succeeded)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                var claims = authentication.Principal!
                    .Claims
                    .GroupBy(claim => claim.Type, StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(claim => claim.Value).ToArray(),
                        StringComparer.Ordinal);
                await context.Response.WriteAsJsonAsync(claims);
            });
        };
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset current = utcNow;

        public override DateTimeOffset GetUtcNow() => current;

        public void Advance(TimeSpan amount) => current = current.Add(amount);
    }
}

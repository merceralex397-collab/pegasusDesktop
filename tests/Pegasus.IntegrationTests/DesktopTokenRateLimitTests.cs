using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Core.Actors;
using Pegasus.Core.Identity;
using Pegasus.Infrastructure.Persistence;
using Pegasus.Web.Api;
using Pegasus.Web.Desktop;
using Pegasus.Web.Mcp;

namespace Pegasus.IntegrationTests;

[Trait("Category", "SqlServer")]
public sealed class DesktopTokenRateLimitTests
{
    private const string Password = "desktop-rate-limit-password";

    [Fact]
    public async Task EleventhPasswordGrantIsRateLimitedAndRecordedWithoutIdentityLockout()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        using var factory = WithDesktopGateway(baseFactory);
        var user = await SeedStaffAsync(factory);
        using var client = CreateClient(factory);

        using var successful = await PostPasswordAsync(client, user, Password);
        Assert.True(successful.IsSuccessStatusCode, await successful.Content.ReadAsStringAsync());

        for (var attempt = 0; attempt < 9; attempt++)
        {
            using var response = await PostPasswordAsync(client, user, "wrong-password");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using var limited = await PostPasswordAsync(client, user, "wrong-password");
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("60", limited.Headers.RetryAfter?.ToString());
        var limitedBody = await limited.Content.ReadAsStringAsync();
        Assert.DoesNotContain("invalid_credentials", limitedBody, StringComparison.Ordinal);
        Assert.DoesNotContain("account-disabled", limitedBody, StringComparison.Ordinal);
        Assert.Equal(
            1,
            await baseFactory.Database.ScalarAsync<int>(
                """
                SELECT COUNT(*) FROM SecurityEvents
                WHERE Type = N'RateLimited'
                  AND Outcome = N'Denied'
                  AND ReasonCode = N'sign_in_rate_limited'
                """));

        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PegasusIdentityUser>>();
        var storedUser = await userManager.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotNull(storedUser);
        Assert.Null(storedUser.LockoutEnd);
    }

    [Fact]
    public async Task AutomationClientCredentialsGrantDoesNotConsumeDesktopSignInBudget()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        using var factory = WithDesktopGateway(baseFactory, automation: true);
        var user = await SeedStaffAsync(factory);
        using var desktop = CreateClient(factory);
        using var automation = CreateClient(factory);

        using var automationResponse = await automation.PostAsync(
            AutomationMcp.TokenEndpointPath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = AutomationMcpTestSupport.ClientId,
                ["client_secret"] = AutomationMcpTestSupport.ClientSecret,
                ["scope"] = AutomationMcp.Scopes[0]
            }));
        var automationBody = await automationResponse.Content.ReadAsStringAsync();
        Assert.True(automationResponse.IsSuccessStatusCode, automationBody);
        using var automationJson = JsonDocument.Parse(automationBody);
        Assert.False(string.IsNullOrWhiteSpace(
            automationJson.RootElement.GetProperty("access_token").GetString()));

        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var response = await PostPasswordAsync(desktop, user, "wrong-password");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using var limited = await PostPasswordAsync(desktop, user, "wrong-password");
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("60", limited.Headers.RetryAfter?.ToString());
    }

    [Fact]
    public async Task DesktopPasswordGrantConsumesTheSharedGlobalSignInBudget()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        using var factory = WithDesktopGateway(baseFactory);
        var user = await SeedStaffAsync(factory);
        using var client = CreateClient(factory);
        var globalLimiter = factory.Services
            .GetRequiredService<System.Threading.RateLimiting.FixedWindowRateLimiter>();

        for (var attempt = 0; attempt < StaffSessionPolicy.SignInAttemptsGlobalPerMinute - 1; attempt++)
        {
            using var lease = await globalLimiter.AcquireAsync(1);
            Assert.True(lease.IsAcquired);
        }

        using var permitted = await PostPasswordAsync(client, user, "wrong-password");
        Assert.Equal(HttpStatusCode.BadRequest, permitted.StatusCode);

        using var limited = await PostPasswordAsync(client, user, "wrong-password");
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
        });

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });

    private static async Task<PegasusIdentityUser> SeedStaffAsync(
        WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var user = new PegasusIdentityUser
        {
            Id = Guid.NewGuid(),
            UserName = $"desktop-rate-limit-{Guid.NewGuid():N}",
            IsEnabled = true,
            MustChangePassword = false,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PegasusIdentityUser>>();
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

    private static Task<HttpResponseMessage> PostPasswordAsync(
        HttpClient client,
        PegasusIdentityUser user,
        string password) =>
        client.PostAsync(
            DesktopSession.TokenEndpointPath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = DesktopSession.ClientId,
                ["username"] = user.UserName!,
                ["password"] = password,
                ["scope"] = DesktopSession.Scope
            }));
}

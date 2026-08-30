using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
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
public sealed partial class DesktopTokenRateLimitTests
{
    private const string Password = "desktop-rate-limit-password";
    private const string TestRemoteIpHeader = "X-Test-Remote-IP";

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
    public async Task AutomationClientCredentialsKeep120PerMinutePolicyAndReasonCode()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        using var factory = WithDesktopGateway(baseFactory, automation: true);
        using var client = CreateClient(factory);
        const int expectedRequestsPerMinute = 120;

        Assert.Equal(expectedRequestsPerMinute, AutomationMcp.RequestsPerClientPerMinute);
        for (var attempt = 0; attempt < expectedRequestsPerMinute; attempt++)
        {
            using var response = await PostAutomationTokenAsync(client);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, body);
        }

        using var limited = await PostAutomationTokenAsync(client);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("60", limited.Headers.RetryAfter?.ToString());
        Assert.Equal(
            1,
            await baseFactory.Database.ScalarAsync<int>(
                """
                SELECT COUNT(*) FROM SecurityEvents
                WHERE Type = N'RateLimited'
                  AND Outcome = N'Denied'
                  AND ReasonCode = N'automation_rate_limited'
                """));
    }

    [Fact]
    public async Task DesktopPasswordGrantConsumesTheSharedGlobalSignInBudget()
    {
        using var baseFactory = new IntakeWebApplicationFactory(useIntegrationTestAuthentication: true);
        using var factory = WithDesktopGateway(baseFactory);
        var user = await SeedStaffAsync(factory);
        using var browser = CreatePipelineClient(factory);
        using var desktop = CreatePipelineClient(factory);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var response = await PostBrowserSignInAsync(
                browser,
                user.UserName!,
                $"198.51.100.{attempt + 1}");
            var browserBody = await response.Content.ReadAsStringAsync();
            // The TestServer handler does not provide the browser cookie jar, so
            // endpoint antiforgery rejects this POST after the global middleware
            // has already consumed its shared sign-in permit.
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, browserBody);
        }

        for (var attempt = 0;
             attempt < StaffSessionPolicy.SignInAttemptsGlobalPerMinute - 11;
             attempt++)
        {
            using var response = await PostPasswordAsync(
                desktop,
                user,
                "wrong-password",
                $"198.51.101.{attempt % 10 + 1}");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using var permitted = await PostPasswordAsync(
            desktop,
            user,
            "wrong-password",
            "198.51.101.10");
        Assert.Equal(HttpStatusCode.BadRequest, permitted.StatusCode);

        using var limited = await PostPasswordAsync(
            desktop,
            user,
            "wrong-password",
            "198.51.101.10");
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("60", limited.Headers.RetryAfter?.ToString());
        Assert.Equal(
            1,
            await baseFactory.Database.ScalarAsync<int>(
                """
                SELECT COUNT(*) FROM SecurityEvents
                WHERE Type = N'RateLimited'
                  AND Outcome = N'Denied'
                  AND ReasonCode = N'sign_in_rate_limited'
                """));
    }

    private static WebApplicationFactory<Program> WithDesktopGateway(
        WebApplicationFactory<Program> baseFactory,
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

    private static HttpClient CreateClient(
        WebApplicationFactory<Program> factory,
        bool handleCookies = false) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = handleCookies,
            BaseAddress = new Uri("https://localhost:7139")
        });

    private static HttpClient CreatePipelineClient(WebApplicationFactory<Program> factory)
    {
        var handler = factory.Server.CreateHandler(context =>
        {
            if (IPAddress.TryParse(
                    context.Request.Headers[TestRemoteIpHeader].FirstOrDefault(),
                    out var remoteIpAddress))
            {
                context.Connection.RemoteIpAddress = remoteIpAddress;
            }
        });
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7139")
        };
    }

    private static async Task<HttpResponseMessage> PostAutomationTokenAsync(HttpClient client)
    {
        return await client.PostAsync(
            AutomationMcp.TokenEndpointPath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = AutomationMcpTestSupport.ClientId,
                ["client_secret"] = AutomationMcpTestSupport.ClientSecret,
                ["scope"] = AutomationMcp.Scopes[0]
            }));
    }

    private static async Task<HttpResponseMessage> PostBrowserSignInAsync(
        HttpClient client,
        string userName,
        string forwardedFor)
    {
        using var signInPageRequest = new HttpRequestMessage(HttpMethod.Get, "/Account/SignIn");
        signInPageRequest.Headers.TryAddWithoutValidation("X-Test-Anonymous", "1");
        using var signInPage = await client.SendAsync(signInPageRequest);
        var signInHtml = await signInPage.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, signInPage.StatusCode);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Account/SignIn")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ReadAntiforgeryToken(signInHtml),
                ["UserName"] = userName,
                ["Password"] = "wrong-password",
                ["ReturnUrl"] = "/"
            })
        };
        request.Headers.TryAddWithoutValidation(TestRemoteIpHeader, forwardedFor);
        if (signInPage.Headers.TryGetValues("Set-Cookie", out var setCookieValues))
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                string.Join(
                    "; ",
                    setCookieValues.Select(value => value.Split(';', 2)[0])));
        }
        return await client.SendAsync(request);
    }

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
        string password,
        string? forwardedFor = null) =>
        SendPasswordRequestAsync(client, user, password, forwardedFor);

    private static async Task<HttpResponseMessage> SendPasswordRequestAsync(
        HttpClient client,
        PegasusIdentityUser user,
        string password,
        string? forwardedFor)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            DesktopSession.TokenEndpointPath)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = DesktopSession.ClientId,
                ["username"] = user.UserName!,
                ["password"] = password,
                ["scope"] = DesktopSession.Scope
            })
        };
        if (forwardedFor is not null)
        {
            request.Headers.TryAddWithoutValidation(TestRemoteIpHeader, forwardedFor);
        }

        return await client.SendAsync(request);
    }

    private static string ReadAntiforgeryToken(string html)
    {
        var tokenTag = AntiforgeryTagRegex().Match(html);
        Assert.True(tokenTag.Success, "The sign-in form must render an antiforgery token.");
        var tokenValue = InputValueRegex().Match(tokenTag.Value);
        Assert.True(tokenValue.Success, "The sign-in antiforgery token must have a value.");
        return WebUtility.HtmlDecode(tokenValue.Groups["value"].Value);
    }

    [GeneratedRegex("<input[^>]*name=\"__RequestVerificationToken\"[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AntiforgeryTagRegex();

    [GeneratedRegex("value=\"(?<value>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InputValueRegex();
}

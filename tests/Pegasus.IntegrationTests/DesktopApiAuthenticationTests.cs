using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Contracts;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Core.Identity;
using Pegasus.Infrastructure.Persistence;
using Pegasus.Web.Api;
using Pegasus.Web.Desktop;
using Pegasus.Web.Mcp;

namespace Pegasus.IntegrationTests;

[Trait("Category", "SqlServer")]
public sealed class DesktopApiAuthenticationTests
{
    private const string Password = "desktop-auth-test-password";
    private const string MailPath = "/api/v1/mail";

    [Fact]
    public async Task ValidBearerTokenUsesTheProductionMailRouteWithEveryAssignedRole()
    {
        await using var database = await LocalDbTestDatabase.CreateAsync(migrate: false);
        using var factory = CreateFactory(database);
        var user = await SeedUserAsync(factory, StaffRoleNames.Engineer, StaffRoleNames.User);
        using var client = CreateClient(factory);
        var token = await RequestDesktopTokenAsync(client, user);

        using var response = await SendGatewayRequestAsync(client, token, MailPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DisablingAccountRejectsTheNextProductionRouteRequestWithTheSameAccessToken()
    {
        await using var database = await LocalDbTestDatabase.CreateAsync(migrate: false);
        using var factory = CreateFactory(database);
        var user = await SeedUserAsync(factory, StaffRoleNames.Engineer);
        using var client = CreateClient(factory);
        var token = await RequestDesktopTokenAsync(client, user);

        await UpdateUserAsync(factory, user.Id, account => account.IsEnabled = false);

        using var response = await SendGatewayRequestAsync(client, token, MailPath);
        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(PegasusProblemTypes.AccountDisabled, problem.Type);
        AssertCorrelation(response, problem);
    }

    [Fact]
    public async Task SecurityStampChangeRejectsTheNextProductionRouteRequestWithTheSameAccessToken()
    {
        await using var database = await LocalDbTestDatabase.CreateAsync(migrate: false);
        using var factory = CreateFactory(database);
        var user = await SeedUserAsync(factory, StaffRoleNames.Engineer);
        using var client = CreateClient(factory);
        var token = await RequestDesktopTokenAsync(client, user);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PegasusIdentityUser>>();
            var trackedUser = await userManager.FindByIdAsync(user.Id.ToString("D"));
            Assert.NotNull(trackedUser);
            var result = await userManager.UpdateSecurityStampAsync(trackedUser!);
            Assert.True(result.Succeeded);
        }

        using var response = await SendGatewayRequestAsync(client, token, MailPath);
        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(PegasusProblemTypes.AccountDisabled, problem.Type);
        Assert.Equal("invalid_security_stamp", problem.Extensions["reasonCode"]?.ToString());
        AssertCorrelation(response, problem);
    }

    [Fact]
    public async Task MustChangePasswordBlocksTheProductionMailRoute()
    {
        await using var database = await LocalDbTestDatabase.CreateAsync(migrate: false);
        using var factory = CreateFactory(database);
        var user = await SeedUserAsync(factory, StaffRoleNames.Engineer, mustChangePassword: true);
        using var client = CreateClient(factory);
        var token = await RequestDesktopTokenAsync(client, user);

        // DSK-03-15 owns the future password-change endpoint; it is not
        // composed here, so this test covers only the production route refusal.
        using var response = await SendGatewayRequestAsync(client, token, MailPath);
        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(PegasusProblemTypes.PasswordChangeRequired, problem.Type);
        AssertCorrelation(response, problem);
    }

    [Fact]
    public async Task AutomationBearerTokenIsRefusedOnTheProductionDesktopRoute()
    {
        await using var database = await LocalDbTestDatabase.CreateAsync(migrate: false);
        using var factory = CreateFactory(database, automationEnabled: true);
        await MigrateDatabaseAsync(factory);
        using var client = CreateClient(factory);
        var token = await AutomationMcpTestSupport.RequestTokenAsync(
            client,
            AutomationMcp.CasesScope);

        using var response = await SendGatewayRequestAsync(client, token, MailPath);
        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(PegasusProblemTypes.NotAuthorized, problem.Type);
        AssertCorrelation(response, problem);
    }

    [Fact]
    public async Task UnknownRoleClaimIsRejectedBeforeTheProductionRouteRuns()
    {
        await using var database = await LocalDbTestDatabase.CreateAsync(migrate: false);
        using var factory = CreateFactory(database);
        var user = await SeedUserAsync(factory, "UnknownRole");
        using var client = CreateClient(factory);
        var token = await RequestDesktopTokenAsync(client, user);

        using var response = await SendGatewayRequestAsync(client, token, MailPath);
        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(PegasusProblemTypes.NotAuthorized, problem.Type);
        AssertCorrelation(response, problem);
    }

    [Fact]
    public async Task BearerChallengeUsesTheProductionRouteAndDoesNotAcceptCookie()
    {
        await using var database = await LocalDbTestDatabase.CreateAsync(migrate: false);
        using var factory = CreateFactory(database);
        await MigrateDatabaseAsync(factory);
        using var client = CreateClient(factory);
        using var request = new HttpRequestMessage(HttpMethod.Get, MailPath);
        request.Headers.Add(PegasusHeaders.CorrelationId, "desktop-auth-challenge");
        request.Headers.Add("Cookie", "__Host-Pegasus=not-a-bearer-token");

        using var response = await client.SendAsync(request);
        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(PegasusProblemTypes.NotAuthorized, problem.Type);
        Assert.Equal("desktop-auth-challenge", problem.CorrelationId);
        Assert.Equal(
            "desktop-auth-challenge",
            response.Headers.GetValues(PegasusHeaders.CorrelationId).Single());
    }

    private static WebApplicationFactory<Program> CreateFactory(
        LocalDbTestDatabase database,
        bool automationEnabled = false)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Runtime:Profile"] = "DevelopmentOffline",
            ["ConnectionStrings:Pegasus"] = database.ConnectionString,
            ["Features:DesktopGateway"] = "true",
            ["Features:LocalIntake"] = "false",
            ["Features:LocalDocumentCustody"] = "false"
        };
        if (automationEnabled)
        {
            settings["Features:AutomationMcp"] = "true";
            settings["AutomationMcp:ClientId"] = AutomationMcpTestSupport.ClientId;
            settings["AutomationMcp:ClientSecret"] = AutomationMcpTestSupport.ClientSecret;
            settings["AutomationMcp:PublicOrigin"] = "http://localhost/";
            settings["AutomationMcp:RegistrationCacheSeconds"] = "0";
        }

        return new ConfiguredWebApplicationFactory("Development", settings);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });

    private static async Task<PegasusIdentityUser> SeedUserAsync(
        WebApplicationFactory<Program> factory,
        params string[] roles) =>
        await SeedUserAsync(factory, roles, mustChangePassword: false);

    private static Task<PegasusIdentityUser> SeedUserAsync(
        WebApplicationFactory<Program> factory,
        string role,
        bool mustChangePassword = false) =>
        SeedUserAsync(factory, [role], mustChangePassword);

    private static async Task<PegasusIdentityUser> SeedUserAsync(
        WebApplicationFactory<Program> factory,
        IReadOnlyList<string> roles,
        bool mustChangePassword)
    {
        await MigrateDatabaseAsync(factory);
        await using var scope = factory.Services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                Assert.True(roleResult.Succeeded);
            }
        }

        var user = new PegasusIdentityUser
        {
            Id = Guid.NewGuid(),
            UserName = $"desktop-auth-{Guid.NewGuid():N}",
            IsEnabled = true,
            MustChangePassword = mustChangePassword,
            LockoutEnabled = false,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PegasusIdentityUser>>();
        var createResult = await userManager.CreateAsync(user, Password);
        Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(error => error.Description)));
        foreach (var role in roles)
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            Assert.True(roleResult.Succeeded);
        }

        return user;
    }

    private static async Task MigrateDatabaseAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PegasusDbContext>();
        await context.Database.MigrateAsync();
    }

    private static async Task UpdateUserAsync(
        WebApplicationFactory<Program> factory,
        Guid userId,
        Action<PegasusIdentityUser> update)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PegasusIdentityUser>>();
        var user = await userManager.FindByIdAsync(userId.ToString("D"));
        Assert.NotNull(user);
        update(user!);
        var result = await userManager.UpdateAsync(user!);
        Assert.True(result.Succeeded);
    }

    private static async Task<string> RequestDesktopTokenAsync(
        HttpClient client,
        PegasusIdentityUser user)
    {
        using var response = await client.PostAsync(
            DesktopSession.TokenEndpointPath,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = DesktopSession.ClientId,
                ["username"] = user.UserName!,
                ["password"] = Password,
                ["scope"] = DesktopSession.Scope
            }));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("access_token").GetString()!;
    }

    private static Task<HttpResponseMessage> SendGatewayRequestAsync(
        HttpClient client,
        string token,
        string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add(PegasusHeaders.CorrelationId, "desktop-auth-test");
        return client.SendAsync(request);
    }

    private static async Task<PegasusProblem> ReadProblemAsync(HttpResponseMessage response)
    {
        var problem = await JsonSerializer.DeserializeAsync<PegasusProblem>(
            await response.Content.ReadAsStreamAsync(),
            PegasusJson.Options);
        Assert.NotNull(problem);
        return problem!;
    }

    private static void AssertCorrelation(HttpResponseMessage response, PegasusProblem problem)
    {
        Assert.Equal("desktop-auth-test", problem.CorrelationId);
        Assert.Equal(
            "desktop-auth-test",
            response.Headers.GetValues(PegasusHeaders.CorrelationId).Single());
    }
}

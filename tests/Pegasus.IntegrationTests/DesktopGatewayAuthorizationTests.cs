using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Pegasus.Contracts;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Core.Identity;
using Pegasus.Infrastructure.Persistence;
using Pegasus.Web.Authentication;
using Pegasus.Web.Api;
using Pegasus.Web.Desktop;

namespace Pegasus.IntegrationTests;

[Trait("Category", "SqlServer")]
public sealed class DesktopGatewayAuthorizationTests
    : IClassFixture<DesktopGatewayAuthorizationFixture>
{
    private readonly DesktopGatewayAuthorizationFixture fixture;

    public DesktopGatewayAuthorizationTests(
        DesktopGatewayAuthorizationFixture fixture)
    {
        this.fixture = fixture;
    }

    public static IEnumerable<object[]> RightMatrix()
    {
        yield return [StaffAccessRight.AccessStaffApplication, "Engineer", false, true];
        yield return [StaffAccessRight.AccessStaffApplication, "User", true, false];
        yield return [StaffAccessRight.PerformCasework, "Engineer", false, true];
        yield return [StaffAccessRight.PerformCasework, "User", true, false];

        foreach (var right in ManagementRights)
        {
            yield return [right, "Administrator", false, true];
            yield return [right, "Engineer", false, false];
        }

        yield return [StaffAccessRight.ExecuteSystemWork, AllStaffRolesCase + "-nominal", false, false];
        yield return [StaffAccessRight.ExecuteSystemWork, AllStaffRolesCase + "-wrong-role", false, false];
        yield return [StaffAccessRight.SubmitRequestUpload, AllStaffRolesCase + "-nominal", false, false];
        yield return [StaffAccessRight.SubmitRequestUpload, AllStaffRolesCase + "-wrong-role", false, false];
    }

    [Theory]
    [MemberData(nameof(RightMatrix))]
    public async Task RealGatewayGroupEnforcesEveryStaffRight(
        StaffAccessRight right,
        string role,
        bool automationAudience,
        bool expectedSuccess)
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        var roles = role.StartsWith(AllStaffRolesCase, StringComparison.Ordinal)
            ? StaffRoles
            : [role];
        foreach (var actualRole in roles)
        {
            var correlationId = $"authorization-{right}-{actualRole}-{Guid.NewGuid():N}";
            using var request = CreateRequest(
                right,
                correlationId,
                actualRole,
                automationAudience);

            using var response = await client.SendAsync(request);

            Assert.Equal(
                expectedSuccess
                    ? HttpStatusCode.NoContent
                    : HttpStatusCode.Forbidden,
                response.StatusCode);
            Assert.Equal(
                correlationId,
                response.Headers.GetValues(PegasusHeaders.CorrelationId).Single());
            if (!expectedSuccess)
            {
                var problem = await ReadProblemAsync(response);
                Assert.Equal(PegasusProblemTypes.NotAuthorized, problem.Type);
                Assert.Equal(correlationId, problem.CorrelationId);
            }
        }
    }

    [Fact]
    public async Task DisabledAccountIsRefusedAndTheDenialJoinsTheResponseCorrelation()
    {
        using var baseFactory = new IntakeWebApplicationFactory(
            useIntegrationTestAuthentication: true);
        using var factory = CreateFactory(baseFactory);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<PegasusIdentityUser>>();
            var user = await userManager.FindByIdAsync(
                DevelopmentOfflineIdentity.AdministratorId.ToString("D"));
            Assert.NotNull(user);
            user!.IsEnabled = false;
            var result = await userManager.UpdateAsync(user);
            Assert.True(result.Succeeded);
        }

        using var client = CreateClient(factory);
        const string correlationId = "authorization-disabled";
        using var response = await client.SendAsync(
            CreateRequest(
                StaffAccessRight.PerformCasework,
                correlationId,
                "Administrator",
                automationAudience: false));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(PegasusProblemTypes.AccountDisabled, problem.Type);
        Assert.Equal(correlationId, problem.CorrelationId);
        Assert.Equal(
            1,
            await baseFactory.Database.ScalarAsync<int>(
                $"""
                SELECT COUNT(*) FROM SecurityEvents
                WHERE SubjectId = '{DevelopmentOfflineIdentity.AdministratorId:D}'
                  AND Type = N'Token'
                  AND Outcome = N'Denied'
                  AND ReasonCode = N'account_disabled'
                  AND CorrelationId = N'{correlationId}'
                """));
    }

    [Fact]
    public async Task AutomationAudienceIsRefusedBeforeTheRightAndRecorded()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        const string correlationId = "authorization-automation";
        using var response = await client.SendAsync(
            CreateRequest(
                StaffAccessRight.PerformCasework,
                correlationId,
                "User",
                automationAudience: true));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(PegasusProblemTypes.NotAuthorized, problem.Type);
        Assert.Equal(correlationId, problem.CorrelationId);
        Assert.Equal(
            1,
            await fixture.Factory.Database.ScalarAsync<int>(
                $"""
                SELECT COUNT(*) FROM SecurityEvents
                WHERE SubjectId = '{DevelopmentOfflineIdentity.AdministratorId:D}'
                  AND Type = N'Token'
                  AND Outcome = N'Denied'
                  AND ReasonCode = N'desktop_token_rejected'
                  AND CorrelationId = N'{correlationId}'
                """));
    }

    [Fact]
    public async Task AnonymousRequestReceivesBearer401()
    {
        using var factory = CreateFactory();
        using var client = CreateClient(factory);
        using var request = CreateRequest(
            StaffAccessRight.PerformCasework,
            "authorization-anonymous",
            "User",
            automationAudience: false);
        request.Headers.Add("X-Test-Anonymous", "1");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(PegasusProblemTypes.NotAuthorized, problem.Type);
        Assert.Equal("authorization-anonymous", problem.CorrelationId);
    }

    private static readonly StaffAccessRight[] ManagementRights =
    [
        StaffAccessRight.ManageStaffAccounts,
        StaffAccessRight.ReviewStaffAccess,
        StaffAccessRight.AssignStaffRoles,
        StaffAccessRight.ManageOrganizationsAndPrincipals,
        StaffAccessRight.ManageWorkflowConfiguration,
        StaffAccessRight.ManageApprovedMailboxes,
        StaffAccessRight.ManageApprovedOutlookCategories,
        StaffAccessRight.ManageAutomationClients
    ];

    private const string AllStaffRolesCase = "AllStaffRoles";
    private static readonly string[] StaffRoles = ["Administrator", "Engineer", "User"];

    private static WebApplicationFactory<Program> CreateFactory(
        IntakeWebApplicationFactory baseFactory)
    {
        return baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(DesktopGateway.FeatureFlag, "true");
            builder.ConfigureServices(services =>
                services.AddSingleton<IStartupFilter, AuthorizationEndpointStartupFilter>());
        });
    }

    private WebApplicationFactory<Program> CreateFactory() => CreateFactory(fixture.Factory);

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });

    private static HttpRequestMessage CreateRequest(
        StaffAccessRight right,
        string correlationId,
        string role,
        bool automationAudience)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{DesktopGateway.BasePath}/__authorization/{right}");
        request.Headers.Add(PegasusHeaders.CorrelationId, correlationId);
        request.Headers.Add("X-Test-Roles", role);
        if (automationAudience)
        {
            request.Headers.Add("X-Test-Automation-Audience", "1");
        }

        return request;
    }

    private static async Task<PegasusProblem> ReadProblemAsync(HttpResponseMessage response)
    {
        var problem = await JsonSerializer.DeserializeAsync<PegasusProblem>(
            await response.Content.ReadAsStreamAsync(),
            PegasusJson.Options);
        Assert.NotNull(problem);
        return problem!;
    }

    private sealed class AuthorizationEndpointStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                next(app);
                app.UseEndpoints(endpoints =>
                {
                    var group = endpoints.MapGroup(DesktopGateway.BasePath)
                        .WithGroupName("v1")
                        .RequireAuthorization(DesktopGateway.AuthorizationPolicy);
                    group.AddEndpointFilter<CorrelationIdEndpointFilter>();
                    group.AddEndpointFilter<ClientVersionEndpointFilter>();
                    group.AddEndpointFilter<DesktopActorResolver>();
                    foreach (var right in Enum.GetValues<StaffAccessRight>())
                    {
                        var rightGroup = group.MapGroup($"/__authorization/{right}");
                        rightGroup.RequireStaffRight(right);
                        rightGroup.MapGet(string.Empty, () => Results.NoContent());
                    }
                });
            };
    }
}

public sealed class DesktopGatewayAuthorizationFixture : IDisposable
{
    public DesktopGatewayAuthorizationFixture()
    {
        Factory = new IntakeWebApplicationFactory(
            useIntegrationTestAuthentication: true);
    }

    public IntakeWebApplicationFactory Factory { get; }

    public void Dispose() => Factory.Dispose();
}

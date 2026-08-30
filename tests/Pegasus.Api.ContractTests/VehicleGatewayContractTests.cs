using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pegasus.Contracts;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Core.Identity;
using Pegasus.Core.Vehicle;
using Pegasus.Web.Api;

namespace Pegasus.Api.ContractTests;

public sealed class VehicleGatewayContractTests
{
    private static readonly Guid CaseId = Guid.Parse("9f45fbe5-2c58-4a92-bf72-df0f2f2e4d01");
    private static readonly Guid ObservationId = Guid.Parse("1d4d10f9-c8ac-4f83-8d2a-5e5bc4a9c9d0");
    private static readonly Guid[] EvidenceObservationIds =
    [
        ObservationId,
        Guid.Parse("6ef69c0a-0a51-4f02-b413-8d8f4f153101"),
        Guid.Parse("6ef69c0a-0a51-4f02-b413-8d8f4f153102"),
        Guid.Parse("6ef69c0a-0a51-4f02-b413-8d8f4f153103"),
        Guid.Parse("6ef69c0a-0a51-4f02-b413-8d8f4f153104"),
        Guid.Parse("6ef69c0a-0a51-4f02-b413-8d8f4f153105"),
        Guid.Parse("6ef69c0a-0a51-4f02-b413-8d8f4f153106")
    ];
    private static readonly Guid[] EvidenceWorkItemIds =
    [
        Guid.Parse("7ef69c0a-0a51-4f02-b413-8d8f4f153101"),
        Guid.Parse("7ef69c0a-0a51-4f02-b413-8d8f4f153102"),
        Guid.Parse("7ef69c0a-0a51-4f02-b413-8d8f4f153103"),
        Guid.Parse("7ef69c0a-0a51-4f02-b413-8d8f4f153104"),
        Guid.Parse("7ef69c0a-0a51-4f02-b413-8d8f4f153105"),
        Guid.Parse("7ef69c0a-0a51-4f02-b413-8d8f4f153106"),
        Guid.Parse("7ef69c0a-0a51-4f02-b413-8d8f4f153107")
    ];

    [Fact]
    public async Task VehicleReadPreservesAllOutcomesProvenanceAndStableEtag()
    {
        using var factory = new VehicleGatewayContractTestFactory();
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Get,
            $"/api/v1/cases/{CaseId:D}/vehicle",
            correlationId: "vehicle-read-correlation");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var responseDocument = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(7, responseDocument.RootElement.GetProperty("version").GetInt64());
        Assert.Equal(
            "vehicle-read-correlation",
            response.Headers.GetValues(PegasusHeaders.CorrelationId).Single());
        var etag = Assert.Single(response.Headers.ETag!.ToString().Split(','));
        Assert.StartsWith("W/\"", etag, StringComparison.Ordinal);

        var document = responseDocument;
        var observations = document.RootElement.GetProperty("observations");
        var outcomes = observations.EnumerateArray()
            .Select(item => item.GetProperty("outcome").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(
            ["current", "failed", "notFound", "partial", "stale", "throttled", "unavailable"],
            outcomes.OrderBy(value => value, StringComparer.Ordinal).ToArray());

        var first = observations.EnumerateArray().First();
        Assert.Equal("dvla-dvsa-replay", first.GetProperty("provenance").GetProperty("provider").GetString());
        Assert.Equal(7200, first.GetProperty("provenance").GetProperty("sourceAgeSeconds").GetInt64());
        Assert.Equal("provider-timeout", observations.EnumerateArray()
            .Single(item => item.GetProperty("outcome").GetString() == "failed")
            .GetProperty("failure").GetProperty("code").GetString());

        using var replayRequest = CreateRequest(
            HttpMethod.Get,
            $"/api/v1/cases/{CaseId:D}/vehicle",
            correlationId: "different-correlation");
        replayRequest.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag[2..], isWeak: true));
        using var replay = await client.SendAsync(replayRequest);
        Assert.Equal(HttpStatusCode.NotModified, replay.StatusCode);
        Assert.Equal(
            "different-correlation",
            replay.Headers.GetValues(PegasusHeaders.CorrelationId).Single());
    }

    [Fact]
    public async Task RequestLookupUsesCoreValidationAndReturnsCorrelation()
    {
        using var factory = new VehicleGatewayContractTestFactory();
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/v1/cases/{CaseId:D}/vehicle/lookups",
            correlationId: "vehicle-request-correlation",
            json: "{\"registration\":\"ab12 cde\",\"expectedVersion\":7,\"operationKey\":\" desk:lookup-1 \",\"editLeaseToken\":\" lease-token \"}");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(
            "vehicle-request-correlation",
            response.Headers.GetValues(PegasusHeaders.CorrelationId).Single());
        Assert.Equal("AB12CDE", factory.LookupStore.Command!.Registration);
        Assert.Equal("desk:lookup-1", factory.LookupStore.Command.OperationKey);
        Assert.Equal("lease-token", factory.LookupStore.Command.EditLeaseToken);
        Assert.Equal(StaffRole.User, factory.LookupStore.Command.Actor.Roles.Single());

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal("pending", document.RootElement.GetProperty("state").GetString());
        Assert.Equal("vehicle-request-correlation", document.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task RequestLookupRequiresExpectedVersion()
    {
        using var factory = new VehicleGatewayContractTestFactory();
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/v1/cases/{CaseId:D}/vehicle/lookups",
            correlationId: "vehicle-missing-version-correlation",
            json: "{\"registration\":\"AB12CDE\",\"operationKey\":\"missing-version\",\"editLeaseToken\":\"lease-token\"}");
        using var response = await client.SendAsync(request);

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, body);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(PegasusProblemTypes.Validation, document.RootElement.GetProperty("type").GetString());
        Assert.Equal("vehicle-missing-version-correlation", document.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task AcceptSuggestionNormalizesCorrectionAndReturnsSafeProvenance()
    {
        using var factory = new VehicleGatewayContractTestFactory();
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/v1/cases/{CaseId:D}/vehicle/suggestions/{ObservationId:D}/accept",
            correlationId: "vehicle-accept-correlation",
            json: "{\"expectedVersion\":8,\"decision\":\"correct\",\"correction\":{\"registration\":\"ab12 cde\",\"make\":\" Ford \",\"model\":\" Focus \",\"mileage\":12345,\"mileageUnit\":\"miles\"},\"operationKey\":\" desk:accept-1 \",\"reason\":\" operator correction \",\"editLeaseToken\":\" lease-token \"}");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("AB12CDE", factory.AcceptStore.Command!.Correction!.Registration);
        Assert.Equal("Ford", factory.AcceptStore.Command.Correction.Make);
        Assert.Equal("operator correction", factory.AcceptStore.Command.Reason);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("x-api-key", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("client-secret", body, StringComparison.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("correct", document.RootElement.GetProperty("decision").GetString());
        Assert.Equal("dvla-dvsa-replay", document.RootElement
            .GetProperty("provenance").GetProperty("provider").GetString());
    }

    [Fact]
    public async Task ProviderUnavailableIsNotFlattenedIntoNotFound()
    {
        using var baseFactory = new VehicleGatewayContractTestFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRequestVehicleLookup>();
                services.AddSingleton<IRequestVehicleLookup, UnavailableLookup>();
            }));
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/v1/cases/{CaseId:D}/vehicle/lookups",
            correlationId: "vehicle-unavailable-correlation",
            json: "{\"registration\":\"AB12CDE\",\"expectedVersion\":7,\"operationKey\":\"desk:unavailable\",\"editLeaseToken\":\"lease-token\"}");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(PegasusProblemTypes.ProviderUnavailable, document.RootElement.GetProperty("type").GetString());
        Assert.NotEqual(PegasusProblemTypes.NotFound, document.RootElement.GetProperty("type").GetString());
        Assert.Equal("vehicle-unavailable-correlation", document.RootElement.GetProperty("correlationId").GetString());
    }

    [Theory]
    [InlineData("operation", 409, PegasusProblemTypes.OperationConflict)]
    [InlineData("suggestion", 404, PegasusProblemTypes.VehicleSuggestionUnavailable)]
    [InlineData("registration-required", 400, PegasusProblemTypes.VehicleRegistrationRequired)]
    [InlineData("registration-conflict", 409, PegasusProblemTypes.VehicleRegistrationConflict)]
    [InlineData("field-conflict", 409, PegasusProblemTypes.VehicleFieldConflict)]
    public async Task VehicleRefusalExceptionsKeepDistinctProblemTypes(
        string refusal,
        int expectedStatus,
        string expectedType)
    {
        using var baseFactory = new VehicleGatewayContractTestFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                if (refusal == "operation")
                {
                    services.RemoveAll<IRequestVehicleLookup>();
                    services.AddSingleton<IRequestVehicleLookup>(
                        new ThrowingLookup(new VehicleOperationConflictException(CaseId, "operation")));
                }
                else
                {
                    Exception exception = refusal switch
                    {
                        "suggestion" => new VehicleSuggestionUnavailableException(
                            ObservationId,
                            VehicleLookupOutcome.Failed),
                        "registration-required" => new ConfirmedVehicleRegistrationRequiredException(CaseId, 0),
                        "registration-conflict" => new ConfirmedVehicleRegistrationConflictException(
                            CaseId,
                            "AB12CDE",
                            "XY34ZAB"),
                        "field-conflict" => new ConfirmedVehicleFieldConflictException(CaseId, "make"),
                        _ => throw new ArgumentOutOfRangeException(nameof(refusal), refusal, null)
                    };
                    services.RemoveAll<IAcceptVehicleSuggestion>();
                    services.AddSingleton<IAcceptVehicleSuggestion>(new ThrowingAccept(exception));
                }
            }));
        using var client = factory.CreateClient();

        var isLookup = refusal == "operation";
        using var request = CreateRequest(
            HttpMethod.Post,
            isLookup
                ? $"/api/v1/cases/{CaseId:D}/vehicle/lookups"
                : $"/api/v1/cases/{CaseId:D}/vehicle/suggestions/{ObservationId:D}/accept",
            correlationId: "vehicle-refusal-correlation",
            json: isLookup
                ? "{\"registration\":\"AB12CDE\",\"expectedVersion\":7,\"operationKey\":\"operation\",\"editLeaseToken\":\"lease-token\"}"
                : "{\"expectedVersion\":7,\"decision\":\"accept\",\"operationKey\":\"operation\",\"reason\":\"reviewed\",\"editLeaseToken\":\"lease-token\"}");
        using var response = await client.SendAsync(request);

        Assert.Equal(expectedStatus, (int)response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(expectedType, document.RootElement.GetProperty("type").GetString());
        Assert.Equal("vehicle-refusal-correlation", document.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task GatewayRequiresAuthenticationAndStaffActor()
    {
        using var factory = new VehicleGatewayContractTestFactory();
        using var client = factory.CreateClient();

        using var unauthenticated = CreateRequest(
            HttpMethod.Get,
            $"/api/v1/cases/{CaseId:D}/vehicle",
            correlationId: "unauthenticated-correlation");
        unauthenticated.Headers.Add("X-Test-Unauthenticated", "true");
        using var unauthenticatedResponse = await client.SendAsync(unauthenticated);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedResponse.StatusCode);

        using var invalidActor = CreateRequest(
            HttpMethod.Get,
            $"/api/v1/cases/{CaseId:D}/vehicle",
            correlationId: "invalid-actor-correlation");
        invalidActor.Headers.Add("X-Test-Invalid-Actor", "true");
        using var invalidActorResponse = await client.SendAsync(invalidActor);
        Assert.Equal(HttpStatusCode.Forbidden, invalidActorResponse.StatusCode);
        using var problem = JsonDocument.Parse(await invalidActorResponse.Content.ReadAsStreamAsync());
        Assert.Equal(PegasusProblemTypes.NotAuthorized, problem.RootElement.GetProperty("type").GetString());
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string path,
        string? correlationId = null,
        string? json = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(PegasusHeaders.ClientVersion, "1.0.0.0");
        if (correlationId is not null)
        {
            request.Headers.Add(PegasusHeaders.CorrelationId, correlationId);
        }
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private sealed class VehicleGatewayContractTestFactory : WebApplicationFactory<Program>
    {
        public LookupStore LookupStore { get; } = new();

        public AcceptStore AcceptStore { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Runtime:Profile", "DevelopmentOffline");
            builder.UseSetting(DesktopGateway.FeatureFlag, "true");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthenticationService>();
                services.AddSingleton<IAuthenticationService, TestAuthenticationService>();
                services.RemoveAll<IRequestVehicleLookupStore>();
                services.AddSingleton<IRequestVehicleLookupStore>(LookupStore);
                services.RemoveAll<IAcceptVehicleSuggestionStore>();
                services.AddSingleton<IAcceptVehicleSuggestionStore>(AcceptStore);
                services.RemoveAll<IVehicleEvidenceQueries>();
                services.AddSingleton<IVehicleEvidenceQueries>(new EvidenceQueries());
            });
        }
    }

    private sealed class TestAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            if (context.Request.Headers.ContainsKey("X-Test-Unauthenticated"))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>();
            if (!context.Request.Headers.ContainsKey("X-Test-Invalid-Actor"))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "4de7c7b0-6119-4b3e-a0ba-b5e8e042c4b0"));
                claims.Add(new Claim(ClaimTypes.Role, StaffRoleNames.User));
            }

            var identity = new ClaimsIdentity(claims, "VehicleGatewayTest");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), scheme ?? "VehicleGatewayTest")));
        }

        public Task ChallengeAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        public Task ForbidAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties) => Task.CompletedTask;

        public Task SignOutAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) => Task.CompletedTask;
    }

    private sealed class LookupStore : IRequestVehicleLookupStore
    {
        public RequestVehicleLookupCommand? Command { get; private set; }

        public Task<RequestedVehicleLookup> RequestAsync(
            RequestVehicleLookupCommand command,
            CancellationToken cancellationToken)
        {
            Command = command;
            return Task.FromResult(new RequestedVehicleLookup(
                Guid.Parse("1a0797ad-3dbc-4276-a8a6-37c01a408d7d"),
                command.CaseId,
                command.Registration,
                VehicleLookupWorkState.Pending,
                command.ExpectedCaseVersion + 1,
                command.CorrelationId,
                IsReplay: false));
        }
    }

    private sealed class AcceptStore : IAcceptVehicleSuggestionStore
    {
        public AcceptVehicleSuggestionCommand? Command { get; private set; }

        public Task<AcceptedVehicleSuggestion> AcceptAsync(
            AcceptVehicleSuggestionCommand command,
            CancellationToken cancellationToken)
        {
            Command = command;
            var values = command.Correction ?? new VehicleConfirmationValues("AB12CDE", "Ford", "Focus", 12345, VehicleMileageUnit.Miles);
            return Task.FromResult(new AcceptedVehicleSuggestion(
                Guid.Parse("9b57d8d7-5a28-4a40-beb1-c1c7f6d6c4f4"),
                command.CaseId,
                command.LookupObservationId,
                command.Decision,
                values,
                Provenance(),
                command.ExpectedCaseVersion + 1,
                "vehicle-accept-correlation",
                IsReplay: false));
        }
    }

    private sealed class EvidenceQueries : IVehicleEvidenceQueries
    {
        public Task<CaseVehicleEvidence?> GetAsync(Guid caseId, CancellationToken cancellationToken) =>
            Task.FromResult<CaseVehicleEvidence?>(caseId == CaseId ? Evidence() : null);
    }

    private sealed class UnavailableLookup : IRequestVehicleLookup
    {
        public Task<RequestedVehicleLookup> ExecuteAsync(
            RequestVehicleLookupCommand command,
            CancellationToken cancellationToken) =>
            throw new VehicleLookupUnavailableException("development_offline_replay");
    }

    private sealed class ThrowingLookup(Exception exception) : IRequestVehicleLookup
    {
        public Task<RequestedVehicleLookup> ExecuteAsync(
            RequestVehicleLookupCommand command,
            CancellationToken cancellationToken) => throw exception;
    }

    private sealed class ThrowingAccept(Exception exception) : IAcceptVehicleSuggestion
    {
        public Task<AcceptedVehicleSuggestion> ExecuteAsync(
            AcceptVehicleSuggestionCommand command,
            CancellationToken cancellationToken) => throw exception;
    }

    private static CaseVehicleEvidence Evidence()
    {
        var outcomes = new[]
        {
            VehicleLookupOutcome.Current,
            VehicleLookupOutcome.Stale,
            VehicleLookupOutcome.Partial,
            VehicleLookupOutcome.NotFound,
            VehicleLookupOutcome.Throttled,
            VehicleLookupOutcome.Unavailable,
            VehicleLookupOutcome.Failed
        };
        var observations = outcomes.Select((outcome, index) => new VehicleLookupObservation(
            EvidenceObservationIds[index],
            EvidenceWorkItemIds[index],
            CaseId,
            1,
            outcome,
            "AB12CDE",
            Provenance(),
            outcome is VehicleLookupOutcome.Current or VehicleLookupOutcome.Stale or VehicleLookupOutcome.Partial
                ? new VehicleDetails("Ford", "Focus", 2020, 1498, "Petrol")
                : null,
            [],
            null,
            outcome switch
            {
                VehicleLookupOutcome.Throttled => new VehicleLookupFailure("rate-limited", true, TimeSpan.FromSeconds(30)),
                VehicleLookupOutcome.Unavailable => new VehicleLookupFailure("provider-unavailable", true),
                VehicleLookupOutcome.Failed => new VehicleLookupFailure("provider-timeout", true),
                _ => null
            },
            new DateTimeOffset(2031, 5, 6, 12, 0, index, TimeSpan.Zero),
            $"vehicle-evidence-correlation-{index}")).ToArray();
        return new(CaseId, null, observations[^1], observations, [], Version: 7);
    }

    private static VehicleEvidenceProvenance Provenance() => new(
        "dvla-dvsa-replay",
        "replay-v1",
        "response-123",
        new DateTimeOffset(2031, 5, 6, 12, 0, 0, TimeSpan.Zero),
        null,
        new DateTimeOffset(2031, 5, 6, 10, 0, 0, TimeSpan.Zero));
}

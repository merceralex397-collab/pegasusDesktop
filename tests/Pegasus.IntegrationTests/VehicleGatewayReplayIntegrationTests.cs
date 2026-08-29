using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Contracts;
using Pegasus.Core.Custody;
using Pegasus.Core.Identity;
using Pegasus.Core.Lifecycle;
using Pegasus.Core.Vehicle;
using Pegasus.Core.Workflow;
using Pegasus.Infrastructure.Persistence;
using Pegasus.Infrastructure.Vehicle;
using Pegasus.Web.Authentication;
using Pegasus.Web.Api;

namespace Pegasus.IntegrationTests;

/// <summary>
/// Exercises the desktop gateway against the real Core use case, SQL stores,
/// worker processor and DevelopmentOffline replay adapter. The fixtures are
/// private temporary files; corpus is not involved.
/// </summary>
[Trait("Category", "SqlServer")]
public sealed class VehicleGatewayReplayIntegrationTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(2031, 5, 6, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task StaffRouteAndAutomaticSweepRemainDistinctThroughWorkerReplay()
    {
        const string staffRegistration = "AB12CDE";
        const string automaticRegistration = "XY34ZAB";
        const string staffCorrelation = "desktop-staff-vehicle-correlation";

        using var baseFactory = new IntakeWebApplicationFactory(useIntegrationTestAuthentication: true);
        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting(DesktopGateway.FeatureFlag, "true"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });

        var staffCaseId = await SeedCaseAsync(
            baseFactory.Database,
            staffRegistration,
            CaseDataCodes.Confirmed,
            withEditLease: true);
        var automaticCaseId = await SeedCaseAsync(
            baseFactory.Database,
            automaticRegistration,
            CaseDataCodes.Fact,
            withEditLease: false);
        var replayRoot = Path.Combine(
            Path.GetTempPath(),
            "Pegasus.IntegrationTests",
            $"vehicle-gateway-replay-{Guid.NewGuid():N}");
        Directory.CreateDirectory(replayRoot);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(replayRoot, $"{staffRegistration}.vehicle-lookup.json"),
                """
                {
                  "schemaVersion": 1,
                  "registration": "AB12CDE",
                  "outcome": "failed",
                  "provider": "dvla-dvsa-replay",
                  "providerVersion": "replay-v1",
                  "responseIdentity": "response-failed",
                  "effectiveAtUtc": null,
                  "sourceObservedAtUtc": null,
                  "vehicle": null,
                  "motTests": [],
                  "failure": { "code": "provider-timeout", "retryable": false }
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(replayRoot, $"{automaticRegistration}.vehicle-lookup.json"),
                """
                {
                  "schemaVersion": 1,
                  "registration": "XY34ZAB",
                  "outcome": "notFound",
                  "provider": "dvla-dvsa-replay",
                  "providerVersion": "replay-v1",
                  "responseIdentity": "response-not-found",
                  "effectiveAtUtc": null,
                  "sourceObservedAtUtc": null,
                  "vehicle": null,
                  "motTests": [],
                  "failure": null
                }
                """);

            using var staffRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/cases/{staffCaseId:D}/vehicle/lookups");
            staffRequest.Headers.Add(PegasusHeaders.ClientVersion, "1.0.0.0");
            staffRequest.Headers.Add(PegasusHeaders.CorrelationId, staffCorrelation);
            staffRequest.Content = new StringContent(
                "{\"registration\":\"AB12CDE\",\"expectedVersion\":0,\"operationKey\":\"desktop-staff-lookup\",\"editLeaseToken\":\"vehicle-edit-lease\"}",
                Encoding.UTF8,
                "application/json");

            using var staffResponse = await client.SendAsync(staffRequest);
            var staffResponseBody = await staffResponse.Content.ReadAsStringAsync();
            Assert.True(
                staffResponse.StatusCode == HttpStatusCode.Accepted,
                staffResponseBody);
            Assert.Equal(
                staffCorrelation,
                staffResponse.Headers.GetValues(PegasusHeaders.CorrelationId).Single());
            using var staffBody = JsonDocument.Parse(staffResponseBody);
            var staffWorkItemId = staffBody.RootElement.GetProperty("workItemId").GetGuid();

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var automatic = scope.ServiceProvider
                    .GetRequiredService<ReconcileAutomaticVehicleLookups>();
                Assert.Equal(1, await automatic.ExecuteAsync(10, CancellationToken.None));
            }

            var automaticWorkItemId = await baseFactory.Database.ScalarAsync<Guid>(
                $"SELECT WorkItemId FROM VehicleLookupRequests WHERE CaseId = '{automaticCaseId:D}'");

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var processor = new ProcessQueuedVehicleLookup(
                    scope.ServiceProvider.GetRequiredService<IVehicleLookupWorkStore>(),
                    new DvlaDvsaReplayAdapter(
                        replayRoot,
                        scope.ServiceProvider.GetRequiredService<TimeProvider>()),
                    scope.ServiceProvider.GetRequiredService<TimeProvider>());
                await processor.ExecuteAsync(staffWorkItemId, CancellationToken.None);
                await processor.ExecuteAsync(automaticWorkItemId, CancellationToken.None);
            }

            Assert.Equal(
                "Staff",
                await baseFactory.Database.ScalarAsync<string>(
                    $"SELECT RequestedByKind FROM VehicleLookupRequests WHERE WorkItemId = '{staffWorkItemId:D}'"));
            Assert.Equal(
                staffCorrelation,
                await baseFactory.Database.ScalarAsync<string>(
                    $"SELECT CorrelationId FROM VehicleLookupRequests WHERE WorkItemId = '{staffWorkItemId:D}'"));
            Assert.Equal(
                "Automation",
                await baseFactory.Database.ScalarAsync<string>(
                    $"SELECT RequestedByKind FROM VehicleLookupRequests WHERE WorkItemId = '{automaticWorkItemId:D}'"));
            Assert.StartsWith(
                "vehicle-lookup:auto:",
                await baseFactory.Database.ScalarAsync<string>(
                    $"SELECT CorrelationId FROM VehicleLookupRequests WHERE WorkItemId = '{automaticWorkItemId:D}'"),
                StringComparison.Ordinal);
            Assert.Equal(
                "error",
                await baseFactory.Database.ScalarAsync<string>(
                    $"SELECT Outcome FROM VehicleLookupObservations WHERE WorkItemId = '{staffWorkItemId:D}'"));
            Assert.Equal(
                "not_found",
                await baseFactory.Database.ScalarAsync<string>(
                    $"SELECT Outcome FROM VehicleLookupObservations WHERE WorkItemId = '{automaticWorkItemId:D}'"));
            Assert.Equal(
                staffCorrelation,
                await baseFactory.Database.ScalarAsync<string>(
                    $"SELECT CorrelationId FROM ActionHistory WHERE AggregateId = '{staffCaseId:D}' AND EventKind = 'vehicle_lookup_error'"));

            using var evidenceRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/cases/{automaticCaseId:D}/vehicle");
            evidenceRequest.Headers.Add(PegasusHeaders.ClientVersion, "1.0.0.0");
            evidenceRequest.Headers.Add(PegasusHeaders.CorrelationId, "desktop-read-correlation");
            using var evidenceResponse = await client.SendAsync(evidenceRequest);
            Assert.Equal(HttpStatusCode.OK, evidenceResponse.StatusCode);
            using var evidenceBody = JsonDocument.Parse(await evidenceResponse.Content.ReadAsStreamAsync());
            Assert.Equal(
                "notFound",
                evidenceBody.RootElement.GetProperty("latestObservation").GetProperty("outcome").GetString());
        }
        finally
        {
            if (Directory.Exists(replayRoot))
            {
                Directory.Delete(replayRoot, recursive: true);
            }
        }
    }

    private static async Task<Guid> SeedCaseAsync(
        LocalDbTestDatabase database,
        string registration,
        string valueKind,
        bool withEditLease)
    {
        var organizationId = Guid.NewGuid();
        var lineageId = Guid.NewGuid();
        var principalId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var leaseToken = "vehicle-edit-lease";
        var leaseHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(leaseToken)));

        await using var context = await database.CreateContextAsync();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Organizations (Id, Name, Version) VALUES ({organizationId}, {$"Vehicle gateway {organizationId:N}"}, {0L})");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO PrincipalSequenceLineages (Id, CreatedAtUtc) VALUES ({lineageId}, {FixedUtcNow})");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Principals (Id, OrganizationId, Code, SequenceLineageId, IsActive, Version) VALUES ({principalId}, {organizationId}, {$"VG{organizationId:N}"[..8].ToUpperInvariant()}, {lineageId}, {true}, {0L})");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO IntakeReceipts (Id, SourceFileName, MediaType, SourceLength, SourceHash, SourceChannel, ExternalReceiptToken, ReceivedAtUtc, ProcessedAtUtc, SourceReaderKey, SourceReaderVersion, Version, Decision, DecisionReason, EvidenceJson, FieldsJson, OcrCandidatesJson) VALUES ({receiptId}, {"vehicle-gateway.eml"}, {"message/rfc822"}, {1L}, {new string('1', 64)}, {"manual_upload"}, {receiptId.ToString("D")}, {FixedUtcNow}, {FixedUtcNow}, {"vehicle-gateway-reader"}, {"1"}, {0L}, {"case_created"}, {"Vehicle gateway fixture"}, {"{\"version\":1,\"data\":[]}"}, {"{\"version\":1,\"data\":[]}"}, {"{\"version\":1,\"data\":[]}"})");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Cases (Id, PrincipalId, SequenceLineageId, Year, Sequence, Reference, Type, InitialState, CustodyState, OriginIntakeReceiptId, InstructionComplete, ImagesComplete, InstructionConfirmedByStaff, ImagesConfirmedByStaff, CreatedAtUtc, Version, ConcurrencyToken) VALUES ({caseId}, {principalId}, {lineageId}, {2031}, {1}, {$"VG-{caseId:N}"[..12].ToUpperInvariant()}, {"inspection"}, {"review"}, {"pending"}, {receiptId}, {true}, {true}, {true}, {true}, {FixedUtcNow}, {0L}, {Guid.NewGuid()})");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO CaseWorkflows (CaseId, State, Version, ConcurrencyToken) VALUES ({caseId}, {CaseLifecycleState.Review.ToString()}, {0L}, {Guid.NewGuid()})");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO CaseDataSnapshots (CaseId, OriginIntakeReceiptId, OriginSourceChannel, OriginExternalReceiptToken, OriginSourceHash, OriginReceivedAtUtc, SourceReaderKey, SourceReaderVersion, CompletenessPolicyKey, CompletenessPolicyVersion, CompletenessPolicySatisfied, AcceptedAtUtc) VALUES ({caseId}, {receiptId}, {"manual_upload"}, {receiptId.ToString("D")}, {new string('1', 64)}, {FixedUtcNow}, {"vehicle-gateway-reader"}, {"1"}, {"vehicle-gateway-completeness"}, {1}, {true}, {FixedUtcNow})");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO CaseDataFields (CaseId, FieldName, ValueKind, ValueType, Value, SourceKind, SourceIdentity, SourceLabel, PolicyKey, PolicyVersion, ConfirmedByActor, ConfirmedAtUtc) VALUES ({caseId}, {"vehicle_registration"}, {valueKind}, {"text"}, {registration}, {"intake_evidence"}, {"vehicle-gateway-source"}, {"Vehicle gateway fixture"}, {"vehicle-gateway-test"}, {1}, {(valueKind == CaseDataCodes.Confirmed ? DevelopmentOfflineIdentity.AdministratorId.ToString("D") : null)}, {(valueKind == CaseDataCodes.Confirmed ? FixedUtcNow : (DateTimeOffset?)null)})");
        if (withEditLease)
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE CaseWorkflows SET EditLeaseToken = {leaseToken}, EditLeaseTokenHash = {leaseHash}, EditLeaseRequestHash = {leaseHash}, EditLeaseHolder = {DevelopmentOfflineIdentity.AdministratorId.ToString("D")}, EditLeaseOperationKey = {"vehicle-gateway-edit"}, EditLeaseExpiresAtUtc = {FixedUtcNow.AddMinutes(5)} WHERE CaseId = {caseId}");
        }

        return caseId;
    }
}

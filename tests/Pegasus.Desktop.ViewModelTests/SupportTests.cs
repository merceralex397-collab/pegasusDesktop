using Pegasus.Contracts.ProblemDetails;
using Pegasus.Desktop.ViewModelTests.Support;

namespace Pegasus.Desktop.ViewModelTests;

public sealed class SupportTests
{
    [Fact]
    [Trait("Category", "ViewModel")]
    public async Task FakeGatewayRecordsRequestsAndReturnsQueuedValues()
    {
        var client = new FakeGatewayClient();
        client.EnqueueResponse(new { Name = "queued" });

        var response = await client.SendAsync(
            "status",
            HttpMethod.Post,
            new Uri("https://localhost/api/v1/status"),
            new { CaseId = "case-1" });

        Assert.True(response.Succeeded);
        Assert.Equal("queued", response.Value?.GetType().GetProperty("Name")?.GetValue(response.Value));
        var request = Assert.Single(client.Requests);
        Assert.Equal("status", request.Operation);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("case-1", request.Body?.GetType().GetProperty("CaseId")?.GetValue(request.Body));
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public async Task FakeGatewayCanReturnEachContractProblemSlug()
    {
        var problemTypes = new[]
        {
            PegasusProblemTypes.Validation,
            PegasusProblemTypes.NotAuthorized,
            PegasusProblemTypes.VersionConflict,
            PegasusProblemTypes.LeaseConflict,
            PegasusProblemTypes.LeaseExpired,
            PegasusProblemTypes.OperationConflict,
            PegasusProblemTypes.ClientUnsupported,
            PegasusProblemTypes.PasswordChangeRequired,
            PegasusProblemTypes.AccountDisabled,
            PegasusProblemTypes.ProviderUnavailable,
            PegasusProblemTypes.NotFound,
            PegasusProblemTypes.RateLimited,
            PegasusProblemTypes.Maintenance
        };
        var client = new FakeGatewayClient();
        foreach (var problemType in problemTypes)
        {
            client.EnqueueProblem(problemType);
        }

        foreach (var expectedType in problemTypes)
        {
            var response = await client.SendAsync("operation");
            Assert.False(response.Succeeded);
            Assert.Equal(expectedType, response.Problem?.Type);
        }
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void FixedTimeProviderIsDeterministicAndAdvances()
    {
        var clock = new FixedTimeProvider();

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), clock.GetUtcNow());
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 5, 0, TimeSpan.Zero), clock.GetUtcNow());
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void FakeCredentialStoreRoundTripsAndClearsValues()
    {
        var store = new FakeCredentialStore();
        store.Save("refresh", "token");

        Assert.True(store.TryRead("refresh", out var value));
        Assert.Equal("token", value);
        Assert.True(store.Clear("refresh"));
        Assert.False(store.TryRead("refresh", out _));
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void FakeNavigationServiceRecordsRoutes()
    {
        var navigation = new FakeNavigationService();

        navigation.Navigate("/dashboard");
        navigation.Navigate("/cases");

        Assert.Equal("/cases", navigation.CurrentRoute);
        Assert.Equal(["/dashboard", "/cases"], navigation.History);
    }
}

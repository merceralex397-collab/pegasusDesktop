using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Contracts;
using Pegasus.Desktop.Infrastructure.Api;
using Pegasus.Desktop.Infrastructure.Authentication;
using Pegasus.Desktop.Infrastructure.Diagnostics;
using Pegasus.Desktop.ViewModelTests.Support;

namespace Pegasus.Desktop.ViewModelTests;

public sealed class Fnd031InfrastructureTests
{
    [Fact]
    [Trait("Category", "ViewModel")]
    public void DpapiCredentialStoreRoundTripsClearsAndDoesNotWritePlaintext()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var store = new DpapiCredentialStore(root);
            const string key = "refresh-token";
            const string secret = "fake-refresh-token-value";

            store.Save(key, secret);

            Assert.True(store.TryRead(key, out var value));
            Assert.Equal(secret, value);

            var protectedBytes = File.ReadAllBytes(Directory.GetFiles(root).Single());
            Assert.False(protectedBytes.AsSpan().SequenceEqual(Encoding.UTF8.GetBytes(secret)));

            store.Clear(key);

            Assert.False(store.TryRead(key, out var clearedValue));
            Assert.Null(clearedValue);
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void DpapiCredentialStoreMissingKeyAndCorruptBlobFailClosed()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var store = new DpapiCredentialStore(root);

            Assert.False(store.TryRead("missing", out var missingValue));
            Assert.Null(missingValue);

            store.Save("corrupt", "fake-value");
            var path = Assert.Single(Directory.GetFiles(root));
            File.WriteAllBytes(path, [0, 1, 2, 3, 4, 5]);

            Assert.Throws<InvalidDataException>(() => store.TryRead("corrupt", out _));
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void DpapiCredentialStoreIsIsolatedByStoreRoot()
    {
        var firstRoot = CreateTemporaryDirectory();
        var secondRoot = CreateTemporaryDirectory();

        try
        {
            var firstStore = new DpapiCredentialStore(firstRoot);
            var secondStore = new DpapiCredentialStore(secondRoot);
            firstStore.Save("session", "first-store-value");

            Assert.False(secondStore.TryRead("session", out var value));
            Assert.Null(value);
        }
        finally
        {
            DeleteTemporaryDirectory(firstRoot);
            DeleteTemporaryDirectory(secondRoot);
        }
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public async Task RequestHandlerAddsVersionAndGeneratesCorrelationScope()
    {
        var primary = new RecordingHttpMessageHandler(
            (_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        var logger = new RecordingLogger<PegasusRequestHandler>();
        using var handler = new PegasusRequestHandler(
            new FixedClientVersionProvider("1.2.3.4"),
            logger)
        {
            InnerHandler = primary
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://gateway.test/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = Assert.Single(primary.Requests);
        var version = Assert.Single(request.Headers.GetValues(PegasusHeaders.ClientVersion));
        var correlation = Assert.Single(request.Headers.GetValues(PegasusHeaders.CorrelationId));
        Assert.Equal("1.2.3.4", version);
        Assert.True(Guid.TryParse(correlation, out _));

        var scope = Assert.IsType<Dictionary<string, object?>>(Assert.Single(logger.Scopes));
        Assert.Equal(correlation, scope[PegasusHeaders.CorrelationId]);
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public async Task RequestHandlerPreservesCallerCorrelationAndReplacesStaleVersion()
    {
        var primary = new RecordingHttpMessageHandler(
            (_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        var logger = new RecordingLogger<PegasusRequestHandler>();
        using var handler = new PegasusRequestHandler(
            new FixedClientVersionProvider("4.3.2.1"),
            logger)
        {
            InnerHandler = primary
        };
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://gateway.test/command");
        request.Headers.TryAddWithoutValidation(PegasusHeaders.CorrelationId, "caller-correlation");
        request.Headers.TryAddWithoutValidation(PegasusHeaders.ClientVersion, "stale-version");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sentRequest = Assert.Single(primary.Requests);
        Assert.Equal("4.3.2.1", Assert.Single(sentRequest.Headers.GetValues(PegasusHeaders.ClientVersion)));
        Assert.Equal("caller-correlation", Assert.Single(sentRequest.Headers.GetValues(PegasusHeaders.CorrelationId)));

        var scope = Assert.IsType<Dictionary<string, object?>>(Assert.Single(logger.Scopes));
        Assert.Equal("caller-correlation", scope[PegasusHeaders.CorrelationId]);
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void ApiClientRegistrationRequiresGatewayBaseAddress()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(
            () => services.AddPegasusApiClient(_ => { }));
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public async Task ApiClientRetriesTransientGetAndPreservesRequestHeaders()
    {
        var primary = new RecordingHttpMessageHandler(
            (_, attempt) => new HttpResponseMessage(
                attempt == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK));
        using var provider = BuildApiServices(primary);
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("pegasus");

        using var response = await client.GetAsync("status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, primary.Requests.Count);
        var firstCorrelation = Assert.Single(
            primary.Requests[0].Headers.GetValues(PegasusHeaders.CorrelationId));
        var secondCorrelation = Assert.Single(
            primary.Requests[1].Headers.GetValues(PegasusHeaders.CorrelationId));
        Assert.Equal(firstCorrelation, secondCorrelation);
        Assert.All(
            primary.Requests,
            request => Assert.Equal(
                "1.0.0.1",
                Assert.Single(request.Headers.GetValues(PegasusHeaders.ClientVersion))));
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public async Task ApiClientStopsAfterThreeTransientGetAttempts()
    {
        var primary = new RecordingHttpMessageHandler(
            (_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var provider = BuildApiServices(primary);
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("pegasus");

        using var response = await client.GetAsync("status");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(3, primary.Requests.Count);
        Assert.All(
            primary.Requests,
            request => Assert.Equal(
                "1.0.0.1",
                Assert.Single(request.Headers.GetValues(PegasusHeaders.ClientVersion))));
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public async Task ApiClientDoesNotRetryCommandPosts()
    {
        var primary = new RecordingHttpMessageHandler(
            (_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var provider = BuildApiServices(primary);
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("pegasus");

        using var response = await client.PostAsync("command", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Single(primary.Requests);
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void DiagnosticsWriterRedactsSensitiveValuesAndPreservesContext()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var writer = new RollingFileDiagnosticsWriter(root, 1024, 2);
            const string context = "operation completed | request-id=fake-request";

            writer.Write(
                $"{context} Authorization: Bearer fake-access-token refresh_token=Bearer fake-refresh-token password=fake-password");

            var content = File.ReadAllText(Assert.Single(writer.GetFiles()));
            Assert.Contains(context, content);
            Assert.Contains("[REDACTED]", content);
            Assert.DoesNotContain("fake-access-token", content);
            Assert.DoesNotContain("fake-refresh-token", content);
            Assert.DoesNotContain("fake-password", content);
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void DiagnosticsWriterRotatesWhenTotalSizeIsExceeded()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var writer = new RollingFileDiagnosticsWriter(root, 150, 2);

            writer.Write("first diagnostic line " + new string('a', 40));
            writer.Write("second diagnostic line " + new string('b', 40));
            writer.Write("third diagnostic line " + new string('c', 40));

            var files = writer.GetFiles();
            var content = string.Join(Environment.NewLine, files.Select(File.ReadAllText));
            Assert.Single(files);
            Assert.DoesNotContain("first diagnostic line", content);
            Assert.Contains("third diagnostic line", content);
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void DiagnosticsWriterHonorsRetentionCount()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var older = Path.Combine(root, "diagnostics-older.log");
            var newer = Path.Combine(root, "diagnostics-newer.log");
            File.WriteAllText(older, "older diagnostic");
            File.WriteAllText(newer, "newer diagnostic");
            var baseTime = new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(older, baseTime);
            File.SetLastWriteTimeUtc(newer, baseTime.AddMinutes(1));

            var writer = new RollingFileDiagnosticsWriter(root, 1024, 2);
            writer.Write("current diagnostic");

            var files = writer.GetFiles();
            var content = string.Join(Environment.NewLine, files.Select(File.ReadAllText));
            Assert.Equal(2, files.Count);
            Assert.DoesNotContain("older diagnostic", content);
            Assert.Contains("newer diagnostic", content);
            Assert.Contains("current diagnostic", content);
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    private static ServiceProvider BuildApiServices(RecordingHttpMessageHandler primary)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IClientVersionProvider>(new FixedClientVersionProvider("1.0.0.1"));
        services.AddPegasusApiClient(options => options.BaseAddress = new Uri("https://gateway.test/"));
        services
            .AddHttpClient("pegasus")
            .ConfigurePrimaryHttpMessageHandler(() => primary);

        return services.BuildServiceProvider();
    }

    private static string CreateTemporaryDirectory() =>
        Directory.CreateTempSubdirectory("pegasus-desktop-viewmodel-tests-").FullName;

    private static void DeleteTemporaryDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}

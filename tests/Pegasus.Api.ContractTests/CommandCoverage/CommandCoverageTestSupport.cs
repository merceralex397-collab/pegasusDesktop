using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Pegasus.Api.ContractTests;

namespace Pegasus.Api.ContractTests.CommandCoverage;

public sealed class CommandCoverageTestContext : IDisposable
{
    private readonly ContractTestWebApplicationFactory factory = new();

    public CommandCoverageTestContext()
    {
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });
    }

    public HttpClient Client { get; }

    public IServiceProvider Services => factory.Services;

    public static HttpRequestMessage CreateJsonRequest(
        string method,
        string path,
        string json,
        string? correlationId = null)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        if (correlationId is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }

        return request;
    }

    public void Dispose()
    {
        Client.Dispose();
        factory.Dispose();
    }
}

internal static class CommandCoverageAssertions
{
    public static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedType,
        string expectedTitle,
        string? expectedCurrentVersion = null)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(expectedType, document.RootElement.GetProperty("type").GetString());
        Assert.Equal(expectedTitle, document.RootElement.GetProperty("title").GetString());
        if (expectedCurrentVersion is not null)
        {
            Assert.Equal(
                expectedCurrentVersion,
                document.RootElement.GetProperty("currentVersion").GetString());
        }
    }

    public static void AssertBearerChallengeOnly(HttpResponseMessage response)
    {
        Assert.NotEmpty(response.Headers.WwwAuthenticate);
        Assert.All(
            response.Headers.WwwAuthenticate,
            challenge => Assert.Equal("Bearer", challenge.Scheme, ignoreCase: true));
    }

    public static async Task AssertResponseBodiesEqualAsync(
        HttpResponseMessage first,
        HttpResponseMessage replay)
    {
        Assert.Equal(first.StatusCode, replay.StatusCode);
        var firstBody = await first.Content.ReadAsStringAsync();
        var replayBody = await replay.Content.ReadAsStringAsync();
        Assert.Equal(firstBody, replayBody);
    }
}

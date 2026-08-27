using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Pegasus.Contracts;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Core.Identity;
using Pegasus.Core.Workflow;
using Pegasus.Web.Api;

namespace Pegasus.IntegrationTests;

public sealed class DesktopGatewayProblemTests
{
    [Fact]
    public Task AuthorizationExceptionsBecomeNotAuthorizedProblems() =>
        AssertKnownExceptionAsync(
            new StaffAuthorizationException(StaffAccessRight.PerformCasework),
            StatusCodes.Status403Forbidden,
            PegasusProblemTypes.NotAuthorized,
            null);

    [Fact]
    public Task VersionConflictsBecomeVersionConflictProblems() =>
        AssertKnownExceptionAsync(
            new CaseVersionConflictException(Guid.NewGuid(), 1, 2),
            StatusCodes.Status409Conflict,
            PegasusProblemTypes.VersionConflict,
            "2");

    [Fact]
    public Task LeaseConflictsBecomeLeaseConflictProblems() =>
        AssertKnownExceptionAsync(
            new CaseEditLeaseConflictException(Guid.NewGuid(), 3),
            StatusCodes.Status409Conflict,
            PegasusProblemTypes.LeaseConflict,
            "3");

    [Fact]
    public Task ExpiredLeasesBecomeLeaseExpiredProblems() =>
        AssertKnownExceptionAsync(
            new CaseEditLeaseExpiredException(Guid.NewGuid(), 4),
            StatusCodes.Status409Conflict,
            PegasusProblemTypes.LeaseExpired,
            "4");

    [Fact]
    public Task OperationConflictsBecomeOperationConflictProblems() =>
        AssertKnownExceptionAsync(
            new CaseOperationConflictException(Guid.NewGuid(), "operation-1"),
            StatusCodes.Status409Conflict,
            PegasusProblemTypes.OperationConflict,
            null);

    [Fact]
    public Task ArgumentExceptionsBecomeValidationProblems() =>
        AssertKnownExceptionAsync(
            new ArgumentException("invalid input"),
            StatusCodes.Status400BadRequest,
            PegasusProblemTypes.Validation,
            null);

    private static async Task AssertKnownExceptionAsync(
        Exception exception,
        int expectedStatus,
        string expectedType,
        string? expectedCurrentVersion)
    {
        var problem = await HandleAsync(exception);

        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal(expectedType, problem.Type);
        Assert.Equal("correlation-from-filter", problem.CorrelationId);
        Assert.Equal(expectedCurrentVersion, problem.CurrentVersion);
    }

    [Fact]
    public async Task InvalidRequestExceptionsBecomeValidationProblems()
    {
        var problem = await HandleAsync(new InvalidDataException("invalid request data"));

        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal(PegasusProblemTypes.Validation, problem.Type);
        Assert.DoesNotContain("invalid request data", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnmappedExceptionsBecomeGenericMaintenanceProblems()
    {
        var problem = await HandleAsync(new NotSupportedException("secret infrastructure detail"));

        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Equal(PegasusProblemTypes.Maintenance, problem.Type);
        Assert.DoesNotContain("secret infrastructure detail", problem.Detail, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("authorization", StatusCodes.Status403Forbidden, PegasusProblemTypes.NotAuthorized)]
    [InlineData("version", StatusCodes.Status409Conflict, PegasusProblemTypes.VersionConflict)]
    [InlineData("lease-conflict", StatusCodes.Status409Conflict, PegasusProblemTypes.LeaseConflict)]
    [InlineData("lease-expired", StatusCodes.Status409Conflict, PegasusProblemTypes.LeaseExpired)]
    [InlineData("operation", StatusCodes.Status409Conflict, PegasusProblemTypes.OperationConflict)]
    [InlineData("validation", StatusCodes.Status400BadRequest, PegasusProblemTypes.Validation)]
    [InlineData("maintenance", StatusCodes.Status500InternalServerError, PegasusProblemTypes.Maintenance)]
    public async Task DevelopmentHostInvokesTheGatewayHandlerForApiExceptions(
        string kind,
        int expectedStatus,
        string expectedType)
    {
        using var baseFactory = new IntakeWebApplicationFactory(
            useIntegrationTestAuthentication: true);
        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(DesktopGateway.FeatureFlag, "true");
            builder.ConfigureServices(services =>
                services.AddSingleton<IStartupFilter, ThrowingEndpointStartupFilter>());
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{DesktopGateway.BasePath}/__throw/{kind}");
        request.Headers.Add(PegasusHeaders.CorrelationId, "host-correlation");

        using var response = await client.SendAsync(request);
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        Assert.Equal(
            "host-correlation",
            response.Headers.GetValues(PegasusHeaders.CorrelationId).Single());
        var problem = await JsonSerializer.DeserializeAsync<PegasusProblem>(
            await response.Content.ReadAsStreamAsync(),
            PegasusJson.Options);
        Assert.NotNull(problem);
        Assert.Equal(expectedType, problem!.Type);
        Assert.Equal("host-correlation", problem.CorrelationId);
    }

    [Fact]
    public async Task CancellationIsLeftToTheHost()
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-cancel"
        };
        httpContext.Response.Body = new MemoryStream();

        var handled = await new DesktopGatewayExceptionHandler().TryHandleAsync(
            httpContext,
            new OperationCanceledException(),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    private static async Task<PegasusProblem> HandleAsync(Exception exception)
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-fallback"
        };
        httpContext.Response.Body = new MemoryStream();
        httpContext.Items[DesktopGatewayRequestContext.CorrelationIdKey] =
            "correlation-from-filter";

        var handled = await new DesktopGatewayExceptionHandler().TryHandleAsync(
            httpContext,
            exception,
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);
        Assert.Equal(
            "correlation-from-filter",
            httpContext.Response.Headers[PegasusHeaders.CorrelationId].ToString());

        httpContext.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<PegasusProblem>(
            httpContext.Response.Body,
            PegasusJson.Options);
        Assert.NotNull(problem);
        return problem!;
    }

    private sealed class ThrowingEndpointStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                next(app);
                app.Use(async (context, nextMiddleware) =>
                {
                    if (context.Request.Path.StartsWithSegments(
                            $"{DesktopGateway.BasePath}/__throw"))
                    {
                        var kind = context.Request.Path.Value!.Split('/').Last();
                        throw CreateException(kind);
                    }

                    await nextMiddleware();
                });
            };

        private static Exception CreateException(string kind) => kind switch
        {
            "authorization" => new StaffAuthorizationException(
                StaffAccessRight.PerformCasework),
            "version" => new CaseVersionConflictException(Guid.NewGuid(), 1, 2),
            "lease-conflict" => new CaseEditLeaseConflictException(Guid.NewGuid(), 3),
            "lease-expired" => new CaseEditLeaseExpiredException(Guid.NewGuid(), 4),
            "operation" => new CaseOperationConflictException(Guid.NewGuid(), "operation-1"),
            "validation" => new ArgumentException("invalid request"),
            "maintenance" => new NotSupportedException("secret infrastructure detail"),
            _ => new InvalidOperationException("unknown test exception")
        };
    }
}

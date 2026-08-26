using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Pegasus.Contracts;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Core.Identity;
using Pegasus.Core.Workflow;
using Pegasus.Web.Api;

namespace Pegasus.IntegrationTests;

public sealed class DesktopGatewayProblemTests
{
    public static TheoryData<Exception, int, string, string?> KnownExceptions => new()
    {
        {
            new StaffAuthorizationException(StaffAccessRight.PerformCasework),
            StatusCodes.Status403Forbidden,
            PegasusProblemTypes.NotAuthorized,
            null
        },
        {
            new CaseVersionConflictException(Guid.NewGuid(), 1, 2),
            StatusCodes.Status409Conflict,
            PegasusProblemTypes.VersionConflict,
            "2"
        },
        {
            new CaseEditLeaseConflictException(Guid.NewGuid(), 3),
            StatusCodes.Status409Conflict,
            PegasusProblemTypes.LeaseConflict,
            "3"
        },
        {
            new CaseEditLeaseExpiredException(Guid.NewGuid(), 4),
            StatusCodes.Status409Conflict,
            PegasusProblemTypes.LeaseExpired,
            "4"
        },
        {
            new CaseOperationConflictException(Guid.NewGuid(), "operation-1"),
            StatusCodes.Status409Conflict,
            PegasusProblemTypes.OperationConflict,
            null
        },
        {
            new ArgumentException("invalid input"),
            StatusCodes.Status400BadRequest,
            PegasusProblemTypes.Validation,
            null
        }
    };

    [Theory]
    [MemberData(nameof(KnownExceptions))]
    public async Task KnownExceptionsBecomeTypedSafeProblems(
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
}

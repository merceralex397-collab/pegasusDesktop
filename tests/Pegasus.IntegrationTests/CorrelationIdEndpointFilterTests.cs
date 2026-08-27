using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Pegasus.Contracts;
using Pegasus.Web.Api;

namespace Pegasus.IntegrationTests;

public sealed class CorrelationIdEndpointFilterTests
{
    [Theory]
    [InlineData("caller-correlation", "caller-correlation")]
    [InlineData("", "trace-filter")]
    [InlineData("invalid\ncorrelation", "trace-filter")]
    public async Task FilterEchoesValidCorrelationOrUsesTraceIdentifier(
        string suppliedCorrelationId,
        string expectedCorrelationId)
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-filter"
        };
        httpContext.Request.Headers[PegasusHeaders.CorrelationId] = suppliedCorrelationId;
        var filter = new CorrelationIdEndpointFilter(
            NullLogger<CorrelationIdEndpointFilter>.Instance);

        var nextCalled = false;
        var result = await filter.InvokeAsync(
            new DefaultEndpointFilterInvocationContext(httpContext, []),
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>("next-result");
            });

        Assert.True(nextCalled);
        Assert.Equal("next-result", result);
        Assert.Equal(
            expectedCorrelationId,
            httpContext.Response.Headers[PegasusHeaders.CorrelationId].ToString());
        Assert.Equal(
            expectedCorrelationId,
            httpContext.Items[DesktopGatewayRequestContext.CorrelationIdKey]);
    }
}

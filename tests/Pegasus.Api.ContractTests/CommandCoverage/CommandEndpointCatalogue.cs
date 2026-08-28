using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Core.Identity;

namespace Pegasus.Api.ContractTests.CommandCoverage;

internal sealed record CommandEndpoint(
    string RoutePattern,
    string Method,
    StaffAccessRight? DeclaredAccessRight);

/// <summary>
/// Discovers command endpoints from the real application's endpoint data
/// source. The catalogue is deliberately derived from routing rather than a
/// second hand-maintained endpoint list.
/// </summary>
internal static class CommandEndpointCatalogue
{
    private static readonly string[] CommandMethods =
    [
        "DELETE",
        "PATCH",
        "POST",
        "PUT"
    ];

    public static IReadOnlyList<CommandEndpoint> Read(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return Read(services.GetRequiredService<EndpointDataSource>().Endpoints);
    }

    internal static IReadOnlyList<CommandEndpoint> Read(IEnumerable<Endpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint => GetCommandMethods(endpoint)
                .Select(method => new CommandEndpoint(
                    GetRoutePattern(endpoint),
                    method,
                    ReadDeclaredAccessRight(endpoint))))
            .OrderBy(endpoint => endpoint.RoutePattern, StringComparer.Ordinal)
            .ThenBy(endpoint => endpoint.Method, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> GetCommandMethods(RouteEndpoint endpoint)
    {
        if (!IsCommandRoute(GetRoutePattern(endpoint)))
        {
            yield break;
        }

        var metadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        if (metadata is null)
        {
            yield break;
        }

        foreach (var method in metadata.HttpMethods
                     .Select(method => method.ToUpperInvariant())
                     .Where(method => CommandMethods.Contains(method, StringComparer.Ordinal))
                     .Distinct(StringComparer.Ordinal)
                     .OrderBy(method => method, StringComparer.Ordinal))
        {
            yield return method;
        }
    }

    private static string GetRoutePattern(RouteEndpoint endpoint) =>
        endpoint.RoutePattern.RawText
        ?? endpoint.RoutePattern.ToString()
        ?? string.Empty;

    private static bool IsCommandRoute(string routePattern) =>
        routePattern.Equals("/api/v1", StringComparison.OrdinalIgnoreCase)
        || routePattern.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase);

    private static StaffAccessRight? ReadDeclaredAccessRight(RouteEndpoint endpoint)
    {
        // GWY-003 owns the production metadata type. Keep this test project
        // independent of that not-yet-landed type while still checking its
        // declared value once it is present on an endpoint.
        foreach (var metadata in endpoint.Metadata)
        {
            var metadataType = metadata.GetType();
            if (!metadataType.Name.Contains("StaffAccessRight", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var property = metadataType.GetProperty("AccessRight")
                ?? metadataType.GetProperty("Right");
            if (property?.PropertyType == typeof(StaffAccessRight)
                && property.GetValue(metadata) is StaffAccessRight accessRight)
            {
                return accessRight;
            }
        }

        return null;
    }
}

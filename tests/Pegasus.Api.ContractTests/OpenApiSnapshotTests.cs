using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Pegasus.Api.ContractTests;

[Collection(nameof(OpenApiSnapshotTestGroup))]
[Trait("Category", "Contract")]
public sealed class OpenApiSnapshotTests
{
    private const string RegenerationCommand = "pwsh ./eng/api/Export-OpenApiDocument.ps1";

    [Fact]
    public async Task OpenApiSnapshotMatchesCommittedDocument()
    {
        using var factory = new ContractTestWebApplicationFactory();
        using var client = factory.CreateClient();
        var document = await client.GetStringAsync("/openapi/v1.json");
        var root = FindRepositoryRoot();
        var snapshotPath = Path.Combine(root, "openapi", "pegasus-v1.json");
        var actualPath = snapshotPath + ".actual";
        var actual = Normalize(document);

        if (!File.Exists(snapshotPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(actualPath)!);
            File.WriteAllBytes(actualPath, actual);
            Assert.Fail(
                $"The OpenAPI snapshot is missing. The generated document was written to {actualPath}. "
                + $"Review it and regenerate with {RegenerationCommand}.");
        }

        var expected = File.ReadAllBytes(snapshotPath);
        Assert.True(actual.SequenceEqual(expected),
            $"The OpenAPI snapshot differs from {snapshotPath}. Review the contract and regenerate with {RegenerationCommand}.");
    }

    [Fact]
    public async Task OpenApiDocumentContainsProblemAndPagingSchemas()
    {
        using var factory = new ContractTestWebApplicationFactory();
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

        var schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("PegasusProblem", out _));
        Assert.True(schemas.TryGetProperty("PagedResult", out _));
    }

    [Fact]
    public async Task DisabledGatewayDoesNotExposeOpenApiDocument()
    {
        using var factory = new DisabledGatewayWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PreviousSnapshotRemainsSatisfied()
    {
        using var factory = new ContractTestWebApplicationFactory();
        using var client = factory.CreateClient();
        using var current = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        using var previous = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(FindRepositoryRoot(), "openapi", "pegasus-v1.previous.json")));

        var currentRoot = current.RootElement;
        var previousPaths = GetOptionalObject(previous.RootElement, "paths");
        if (previousPaths is null)
        {
            // The first accepted snapshot has no endpoints, so this fact is
            // intentionally vacuous until a path is added to that snapshot.
            return;
        }

        var currentPaths = GetOptionalObject(currentRoot, "paths");
        Assert.True(currentPaths is not null, "The current OpenAPI document has no paths object.");

        foreach (var previousPath in previousPaths.Value.EnumerateObject())
        {
            Assert.True(currentPaths.Value.TryGetProperty(previousPath.Name, out var currentPath),
                $"The current OpenAPI document removed path '{previousPath.Name}'.");

            foreach (var previousOperation in previousPath.Value.EnumerateObject()
                         .Where(property => IsHttpOperation(property.Name)))
            {
                Assert.True(currentPath.TryGetProperty(previousOperation.Name, out var currentOperation),
                    $"The current OpenAPI document removed operation '{previousOperation.Name.ToUpperInvariant()} {previousPath.Name}'.");
                AssertPreviousResponsesRemain(
                    previousOperation.Value,
                    currentOperation,
                    previous.RootElement,
                    currentRoot,
                    previousPath.Name,
                    previousOperation.Name);
            }
        }
    }

    private static void AssertPreviousResponsesRemain(
        JsonElement previousOperation,
        JsonElement currentOperation,
        JsonElement previousRoot,
        JsonElement currentRoot,
        string path,
        string method)
    {
        var previousResponses = GetOptionalObject(previousOperation, "responses");
        if (previousResponses is null)
        {
            return;
        }

        var currentResponses = GetOptionalObject(currentOperation, "responses");
        Assert.True(currentResponses is not null,
            $"The current OpenAPI document removed all responses from '{method.ToUpperInvariant()} {path}'.");

        foreach (var previousResponse in previousResponses.Value.EnumerateObject())
        {
            Assert.True(currentResponses.Value.TryGetProperty(previousResponse.Name, out var currentResponse),
                $"The current OpenAPI document removed response '{previousResponse.Name}' from '{method.ToUpperInvariant()} {path}'.");

            var previousContent = GetOptionalObject(previousResponse.Value, "content");
            var currentContent = GetOptionalObject(currentResponse, "content");
            if (previousContent is null)
            {
                continue;
            }

            Assert.True(currentContent is not null,
                $"The current OpenAPI document removed response content from '{method.ToUpperInvariant()} {path}'.");
            foreach (var previousMediaType in previousContent.Value.EnumerateObject())
            {
                Assert.True(currentContent.Value.TryGetProperty(previousMediaType.Name, out var currentMediaType),
                    $"The current OpenAPI document removed response media type '{previousMediaType.Name}' from '{method.ToUpperInvariant()} {path}'.");

                var previousSchema = GetOptionalObject(previousMediaType.Value, "schema");
                if (previousSchema is null)
                {
                    continue;
                }

                var currentSchema = GetOptionalObject(currentMediaType, "schema");
                Assert.True(currentSchema is not null,
                    $"The current OpenAPI document removed the response schema from '{method.ToUpperInvariant()} {path}'.");
                AssertRequiredPropertiesRemain(
                    previousSchema.Value,
                    currentSchema.Value,
                    previousRoot,
                    currentRoot,
                    path,
                    method);
            }
        }
    }

    private static void AssertRequiredPropertiesRemain(
        JsonElement previousSchema,
        JsonElement currentSchema,
        JsonElement previousRoot,
        JsonElement currentRoot,
        string path,
        string method)
    {
        var previousResolved = ResolveSchema(previousSchema, previousRoot);
        var currentResolved = ResolveSchema(currentSchema, currentRoot);
        var previousRequired = GetOptionalArray(previousResolved, "required");
        var currentRequired = GetOptionalArray(currentResolved, "required")?
            .EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => value.GetString()!)
            .ToHashSet(StringComparer.Ordinal)
            ?? [];
        var currentProperties = GetOptionalObject(currentResolved, "properties");
        if (previousRequired is not null)
        {
            foreach (var property in previousRequired.Value.EnumerateArray())
            {
                var propertyName = property.GetString()!;
                Assert.True(currentRequired.Contains(propertyName),
                    $"The current OpenAPI document removed required response property '{propertyName}' from '{method.ToUpperInvariant()} {path}'.");
                Assert.True(currentProperties is not null
                    && currentProperties.Value.TryGetProperty(propertyName, out _),
                    $"The current OpenAPI document removed required response property schema '{propertyName}' from '{method.ToUpperInvariant()} {path}'.");
            }
        }

        var previousProperties = GetOptionalObject(previousResolved, "properties");
        if (previousProperties is not null)
        {
            Assert.True(currentProperties is not null,
                $"The current OpenAPI document removed response properties from '{method.ToUpperInvariant()} {path}'.");
            foreach (var previousProperty in previousProperties.Value.EnumerateObject())
            {
                if (currentProperties.Value.TryGetProperty(previousProperty.Name, out var currentProperty))
                {
                    AssertRequiredPropertiesRemain(
                        previousProperty.Value,
                        currentProperty,
                        previousRoot,
                        currentRoot,
                        path,
                        method);
                }
            }
        }

        var previousItems = GetOptionalObject(previousResolved, "items");
        var currentItems = GetOptionalObject(currentResolved, "items");
        if (previousItems is not null && currentItems is not null)
        {
            AssertRequiredPropertiesRemain(
                previousItems.Value,
                currentItems.Value,
                previousRoot,
                currentRoot,
                path,
                method);
        }
    }

    private static JsonElement ResolveSchema(JsonElement schema, JsonElement documentRoot)
    {
        var resolved = schema;
        while (resolved.ValueKind == JsonValueKind.Object
               && resolved.TryGetProperty("$ref", out var reference))
        {
            var referenceName = reference.GetString()!.Split('/').Last();
            if (!documentRoot.TryGetProperty("components", out var components)
                || !components.TryGetProperty("schemas", out var schemas))
            {
                return resolved;
            }

            if (!schemas.TryGetProperty(referenceName, out resolved))
            {
                return resolved;
            }
        }

        return resolved;
    }

    private static JsonElement? GetOptionalObject(JsonElement value, string propertyName) =>
        value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Object
            ? property
            : null;

    private static JsonElement? GetOptionalArray(JsonElement value, string propertyName) =>
        value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Array
            ? property
            : null;

    private static bool IsHttpOperation(string propertyName) => propertyName switch
    {
        "get" or "put" or "post" or "delete" or "options" or "head" or "patch" or "trace" => true,
        _ => false
    };

    private static byte[] Normalize(string json)
    {
        using var document = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            WriteElement(writer, document.RootElement, isRoot: true);
        }

        return Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n");
    }

    private static void WriteElement(Utf8JsonWriter writer, JsonElement element, bool isRoot = false)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject()
                             .Where(property => !isRoot || property.Name != "servers")
                             .OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteElement(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteElement(writer, item);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText(), skipInputValidation: true);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                throw new JsonException($"Unsupported JSON value kind: {element.ValueKind}.");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Pegasus.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the Pegasus repository root.");
    }

    private sealed class DisabledGatewayWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Runtime:Profile", "DevelopmentOffline");
            builder.UseSetting("Features:DesktopGateway", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPolicyEvaluator>();
                services.AddSingleton<IPolicyEvaluator, AllowAllPolicyEvaluator>();
            });
        }
    }

    private sealed class AllowAllPolicyEvaluator : IPolicyEvaluator
    {
        public Task<AuthenticateResult> AuthenticateAsync(
            AuthorizationPolicy policy,
            HttpContext context) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task<PolicyAuthorizationResult> AuthorizeAsync(
            AuthorizationPolicy policy,
            AuthenticateResult authenticateResult,
            HttpContext context,
            object? resource) =>
            Task.FromResult(PolicyAuthorizationResult.Success());
    }
}

[CollectionDefinition(nameof(OpenApiSnapshotTestGroup), DisableParallelization = true)]
public sealed class OpenApiSnapshotTestGroup
{
}

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Pegasus.Web.Api;

internal sealed class VehicleOpenApiOperationTransformer : IOpenApiOperationTransformer
{
    private static readonly string[] LookupOutcomes =
    [
        "current",
        "stale",
        "partial",
        "notFound",
        "throttled",
        "unavailable",
        "failed"
    ];

    private static readonly string[] LookupStates =
    [
        "pending",
        "processing",
        "retryScheduled",
        "completed",
        "failed",
        "poisoned"
    ];

    private static readonly string[] Decisions = ["accept", "correct"];

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Document is not { } document)
        {
            return Task.CompletedTask;
        }

        switch (operation.OperationId)
        {
            case "RequestVehicleLookup":
                MarkRequiredExpectedVersion(document, "VehicleLookupRequest");
                SetEnum(document, "VehicleLookupResponse", "state", LookupStates);
                break;
            case "AcceptVehicleSuggestion":
                MarkRequiredExpectedVersion(document, "AcceptVehicleSuggestionRequest");
                SetEnum(document, "AcceptVehicleSuggestionRequest", "decision", Decisions);
                SetEnum(document, "AcceptedVehicleSuggestionResponse", "decision", Decisions);
                break;
            case "GetVehicleEvidence":
                SetEnum(document, "VehicleLookupObservationResponse", "outcome", LookupOutcomes);
                AddConditionalGetMetadata(operation, "vehicle evidence");
                break;
            case "ListMail":
            case "SearchDeletedMail":
            case "PreviewMail":
            case "GetMail":
                AddConditionalGetMetadata(operation, "mail");
                break;
        }

        return Task.CompletedTask;
    }

    private static void MarkRequiredExpectedVersion(OpenApiDocument document, string schemaName)
    {
        if (document.Components?.Schemas is not { } schemas
            || !schemas.TryGetValue(schemaName, out var schema)
            || schema is not OpenApiSchema openApiSchema)
        {
            return;
        }

        openApiSchema.Required ??= new HashSet<string>(StringComparer.Ordinal);
        openApiSchema.Required.Add("expectedVersion");
        if (openApiSchema.Properties is { } properties
            && properties.TryGetValue("expectedVersion", out var expectedVersion)
            && expectedVersion is OpenApiSchema expectedVersionSchema)
        {
            expectedVersionSchema.Type = JsonSchemaType.Integer | JsonSchemaType.String;
        }
    }

    private static void SetEnum(
        OpenApiDocument document,
        string schemaName,
        string propertyName,
        IEnumerable<string> values)
    {
        if (document.Components?.Schemas is not { } schemas
            || !schemas.TryGetValue(schemaName, out var schema)
            || schema is not OpenApiSchema openApiSchema
            || openApiSchema.Properties is not { } properties
            || !properties.TryGetValue(propertyName, out var property)
            || property is not OpenApiSchema propertySchema)
        {
            return;
        }

        propertySchema.Enum = values
            .Select(value => (JsonNode)JsonValue.Create(value)!)
            .ToList();
    }

    private static void AddConditionalGetMetadata(OpenApiOperation operation, string projectionName)
    {
        var parameters = operation.Parameters ??= [];
        if (!parameters.Any(parameter =>
                string.Equals(parameter.Name, "If-None-Match", StringComparison.OrdinalIgnoreCase)
                && parameter.In == ParameterLocation.Header))
        {
            parameters.Add(new OpenApiParameter
            {
                Name = "If-None-Match",
                In = ParameterLocation.Header,
                Description = $"The weak {projectionName} version previously returned by this endpoint.",
                Required = false,
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            });
        }

        if (operation.Responses is not { } responses)
        {
            return;
        }

        foreach (var status in new[] { "200", "304" })
        {
            if (!responses.TryGetValue(status, out var response)
                || response is not OpenApiResponse openApiResponse)
            {
                continue;
            }

            openApiResponse.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.OrdinalIgnoreCase);
            openApiResponse.Headers["ETag"] = new OpenApiHeader
            {
                Description = $"Weak version of the {projectionName} projection.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            };
        }
    }
}

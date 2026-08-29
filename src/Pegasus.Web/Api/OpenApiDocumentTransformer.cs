using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Pegasus.Contracts.Paging;
using Pegasus.Contracts.ProblemDetails;

namespace Pegasus.Web.Api;

internal sealed class OpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var productVersion = GetProductVersion();
        document.Info = new OpenApiInfo
        {
            Title = "Pegasus Desktop Gateway",
            Version = productVersion,
            Description = $"Versioned JSON API for the Pegasus native desktop (product version {productVersion})."
        };

        document.AddComponent("PegasusProblem", CreateProblemSchema());

        var pagingSchema = await context.GetOrCreateSchemaAsync(
            typeof(PagedResult<object>),
            parameterDescription: null,
            cancellationToken);
        document.AddComponent("PagedResult", pagingSchema);
    }

    private static string GetProductVersion()
    {
        var informationalVersion = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? throw new InvalidOperationException("Assembly informational version is required.");
        var separator = informationalVersion.IndexOf('+', StringComparison.Ordinal);

        return separator > 0
            ? informationalVersion[..separator]
            : informationalVersion;
    }

    private static OpenApiSchema CreateProblemSchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Description = "RFC 9457 problem details returned by the desktop gateway.",
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["type"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["title"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer },
                ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["instance"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["correlationId"] = new OpenApiSchema { Type = JsonSchemaType.String }
            },
            Required = new HashSet<string>
            {
                "type",
                "title",
                "status",
                "correlationId"
            }
        };
    }
}

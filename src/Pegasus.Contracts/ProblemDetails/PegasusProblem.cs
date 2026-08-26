using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pegasus.Contracts.ProblemDetails;

/// <summary>
/// RFC 9457 problem details with a correlation identifier and typed version
/// extensions. The body never carries payload dumps or infrastructure detail;
/// this is the boundary documented by
/// <c>src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:7-15</c>.
/// </summary>
[JsonConverter(typeof(PegasusProblemJsonConverter))]
public sealed record PegasusProblem
{
    public PegasusProblem()
    {
    }

    public PegasusProblem(
        string type,
        string title,
        int status,
        string? detail,
        string? instance,
        string correlationId,
        Dictionary<string, object?>? extensions = null)
    {
        Type = type;
        Title = title;
        Status = status;
        Detail = detail;
        Instance = instance;
        CorrelationId = correlationId;
        Extensions = extensions ?? [];
    }

    public string Type { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public int Status { get; init; }

    public string? Detail { get; init; }

    public string? Instance { get; init; }

    public string CorrelationId { get; init; } = string.Empty;

    [JsonIgnore]
    public Dictionary<string, object?> Extensions { get; set; } = [];

    /// <summary>
    /// The server's current version for a version-conflict response. This is
    /// typed because <c>CaseVersionConflictException</c> exposes
    /// <c>ActualVersion</c>, while both lease exceptions expose
    /// <c>CaseVersion</c> (<c>CaseWorkflowContracts.cs:125-149</c>).
    /// </summary>
    [JsonIgnore]
    public string? CurrentVersion => GetExtensionString("currentVersion");

    /// <summary>The minimum client version accepted by the server.</summary>
    [JsonIgnore]
    public string? MinimumVersion => GetExtensionString("minimumVersion");

    private string? GetExtensionString(string name)
    {
        if (!Extensions.TryGetValue(name, out var value))
        {
            return null;
        }

        return value switch
        {
            string text => text,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            _ => null
        };
    }
}

internal sealed class PegasusProblemJsonConverter : JsonConverter<PegasusProblem>
{
    private static readonly HashSet<string> StandardProperties =
    [
        "type",
        "title",
        "status",
        "detail",
        "instance",
        "correlationId"
    ];

    public override PegasusProblem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var extensions = new Dictionary<string, object?>();

        foreach (var property in root.EnumerateObject())
        {
            if (!StandardProperties.Contains(property.Name))
            {
                extensions[property.Name] = property.Value.Clone();
            }
        }

        return new PegasusProblem(
            root.GetProperty("type").GetString()!,
            root.GetProperty("title").GetString()!,
            root.GetProperty("status").GetInt32(),
            GetNullableString(root, "detail"),
            GetNullableString(root, "instance"),
            root.GetProperty("correlationId").GetString()!,
            extensions);
    }

    public override void Write(Utf8JsonWriter writer, PegasusProblem value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);
        writer.WriteString("title", value.Title);
        writer.WriteNumber("status", value.Status);
        WriteNullableString(writer, "detail", value.Detail, options);
        WriteNullableString(writer, "instance", value.Instance, options);
        writer.WriteString("correlationId", value.CorrelationId);

        foreach (var extension in value.Extensions)
        {
            if (StandardProperties.Contains(extension.Key) ||
                (extension.Value is null && options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull))
            {
                continue;
            }

            writer.WritePropertyName(extension.Key);
            JsonSerializer.Serialize(writer, extension.Value, options);
        }

        writer.WriteEndObject();
    }

    private static string? GetNullableString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetString()
            : null;
    }

    private static void WriteNullableString(
        Utf8JsonWriter writer,
        string name,
        string? value,
        JsonSerializerOptions options)
    {
        if (value is not null)
        {
            writer.WriteString(name, value);
        }
        else if (options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingNull)
        {
            writer.WriteNull(name);
        }
    }
}

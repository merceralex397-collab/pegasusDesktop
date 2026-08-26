using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pegasus.Contracts;

public static class PegasusJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

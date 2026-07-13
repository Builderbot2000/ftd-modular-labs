using System.Text.Json;
using System.Text.Json.Serialization;

namespace FtdModularLabs.Domain.Serialization;

/// <summary>Shared <see cref="JsonSerializerOptions"/> for all persisted domain documents.</summary>
public static class DomainJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
}

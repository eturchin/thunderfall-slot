using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Thunderfall.Engine.Abstractions.Definitions;

public static class DefinitionJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        AllowOutOfOrderMetadataProperties = true,
        Converters = { new JsonStringEnumConverter() },
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static GameDefinition Load(string path)
    {
        var g =
            JsonSerializer.Deserialize<GameDefinition>(File.ReadAllText(path), Options)
            ?? throw new ArgumentException("Empty definition");
        DefinitionValidator.Validate(g);
        return g;
    }

    public static string Hash(GameDefinition definition)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            Canonical(JsonSerializer.SerializeToElement(definition, Options), writer);
        }

        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }

    private static void Canonical(JsonElement e, Utf8JsonWriter w)
    {
        if (e.ValueKind == JsonValueKind.Object)
        {
            w.WriteStartObject();
            foreach (var p in e.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                w.WritePropertyName(p.Name);
                Canonical(p.Value, w);
            }
            w.WriteEndObject();
        }
        else if (e.ValueKind == JsonValueKind.Array)
        {
            w.WriteStartArray();
            foreach (var v in e.EnumerateArray())
            {
                Canonical(v, w);
            }

            w.WriteEndArray();
        }
        else
        {
            e.WriteTo(w);
        }
    }
}

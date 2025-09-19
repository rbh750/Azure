using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common.Helpers.Serializers;

public class EnumCollectionConverter<T> : JsonConverter<List<T>?> where T : struct, Enum
{
    private readonly StringEnumConverter _enumConverter = new();

    public override void WriteJson(JsonWriter writer, List<T>? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartArray();
        foreach (var item in value)
        {
            _enumConverter.WriteJson(writer, item, serializer);
        }
        writer.WriteEndArray();
    }

    public override List<T>? ReadJson(JsonReader reader, Type objectType, List<T>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonSerializationException($"Expected StartArray token, got {reader.TokenType}");

        var result = new List<T>();
        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
        {
            var enumValue = (T?)_enumConverter.ReadJson(reader, typeof(T), null, serializer);
            if (enumValue.HasValue)
                result.Add(enumValue.Value);
        }

        return result;
    }
}
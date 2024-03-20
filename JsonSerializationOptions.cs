using System.Text.Json;
using System.Text.Json.Serialization;
using SnapApp.Svc.Extensions;

namespace SnapApp.Svc
{
    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.TitleToSnakeCase();
        }
    }

    public class BoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TryGetInt32(out int intValue))
                return intValue != 0;

            throw new JsonException("Expected an integer value.");
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

    public static class JsonSerializationOptions
    {
        public static JsonSerializerOptions SnakeCaseNamingOptions => new()
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false)
            }
        };

        public static JsonSerializerOptions CamelCaseNamingOptions => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false)
            }
        };
    }
}
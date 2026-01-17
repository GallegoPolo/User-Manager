using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuthService.Api.Converters
{
    public class EnumDescriptionJsonConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonValue = reader.GetString();

            if (string.IsNullOrEmpty(jsonValue))
                throw new JsonException($"Cannot convert null or empty string to {typeof(T).Name}");

            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attribute = field.GetCustomAttribute<DescriptionAttribute>();

                if (attribute != null && attribute.Description.Equals(jsonValue, StringComparison.OrdinalIgnoreCase))
                {
                    return (T)field.GetValue(null)!;
                }
            }

            if (Enum.TryParse<T>(jsonValue, ignoreCase: true, out var result))
            {
                return result;
            }

            throw new JsonException($"Cannot convert '{jsonValue}' to {typeof(T).Name}");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var field = value.GetType().GetField(value.ToString()!);

            if (field == null)
            {
                writer.WriteStringValue(value.ToString());
                return;
            }

            var attribute = field.GetCustomAttribute<DescriptionAttribute>();

            var description = attribute?.Description ?? value.ToString();

            if (options.PropertyNamingPolicy == JsonNamingPolicy.CamelCase && description != null)
                description = JsonNamingPolicy.CamelCase.ConvertName(description);

            writer.WriteStringValue(description);
        }
    }
}

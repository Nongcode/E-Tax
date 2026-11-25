using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace E_Tax
{
    /// <summary>
    /// Đọc số hoặc chuỗi thành chuỗi; kiểu khác (object, array, bool) bị bỏ qua và trả null.
    /// </summary>
    public sealed class FlexibleStringConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    return reader.TryGetInt64(out long l)
                        ? l.ToString(CultureInfo.InvariantCulture)
                        : reader.GetDouble().ToString(CultureInfo.InvariantCulture);
                case JsonTokenType.Null:
                    return null;
                default:
                    reader.Skip();
                    return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value == null) writer.WriteNullValue();
            else writer.WriteStringValue(value);
        }
    }
}

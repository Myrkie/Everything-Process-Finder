using System.Text.Json;
using System.Text.Json.Serialization;

namespace Everything_Process_Finder.Configuration
{
    public class SafeColorConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            byte r = 0, g = 0, b = 0, a = 255;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                string? propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "R": r = reader.GetByte(); break;
                    case "G": g = reader.GetByte(); break;
                    case "B": b = reader.GetByte(); break;
                    case "A": a = reader.GetByte(); break;
                    default: reader.Skip(); break;
                }
            }

            return Color.FromArgb(a, r, g, b);
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("R", value.R);
            writer.WriteNumber("G", value.G);
            writer.WriteNumber("B", value.B);
            writer.WriteNumber("A", value.A);
            writer.WriteEndObject();
        }
    }
}
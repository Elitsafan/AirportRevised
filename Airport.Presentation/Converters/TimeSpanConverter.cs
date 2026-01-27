using Newtonsoft.Json;

namespace Airport.Presentation.Converters
{
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (!TimeSpan.TryParse((string?)reader.Value, out TimeSpan result))
                throw new ArgumentException("Cannot parse time.");
            return result;
        }

        public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer) =>
            writer.WriteValue(value.ToString());
    }
}
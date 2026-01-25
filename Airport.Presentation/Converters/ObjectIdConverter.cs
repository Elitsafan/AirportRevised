using MongoDB.Bson;
using Newtonsoft.Json;

namespace Airport.Presentation.Converters
{
    public class ObjectIdConverter : JsonConverter<ObjectId>
    {
        public override ObjectId ReadJson(JsonReader reader, Type objectType, ObjectId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (!ObjectId.TryParse((string?)reader.Value, out ObjectId result))
                throw new ArgumentException("Cannot parse Id.");
            return result;
        }

        public override void WriteJson(JsonWriter writer, ObjectId value, JsonSerializer serializer) => 
            writer.WriteValue(value.ToString());
    }
}

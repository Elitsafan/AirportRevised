using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Airport.Models.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FlightType
    {
        Landing = 0,
        Departure = 1
    }
}

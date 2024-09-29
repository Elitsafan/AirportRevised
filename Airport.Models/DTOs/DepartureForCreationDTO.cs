using Airport.Models.Enums;

namespace Airport.Models.DTOs
{
    public class DepartureForCreationDTO : FlightForCreationDTO
    {
        public override FlightType FlightType => FlightType.Departure;
    }
}

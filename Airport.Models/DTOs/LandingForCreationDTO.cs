using Airport.Models.Enums;

namespace Airport.Models.DTOs
{
    public class LandingForCreationDTO : FlightForCreationDTO
    {
        public override FlightType FlightType => FlightType.Landing;
    }
}

namespace Airport.Models.DTOs
{
    public class StationForCreationDTO : StationForOperationDTO
    {
        public override FlightDTO? Flight { get; set; }
        public override TimeSpan WaitingTime { get; set; }
    }
}

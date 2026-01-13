namespace Airport.Models.DTOs
{
    public class StationForUpdateDTO : StationForOperationDTO
    {
        public override FlightDTO? Flight { get; set; }
        public override TimeSpan WaitingTime { get; set; }
    }
}

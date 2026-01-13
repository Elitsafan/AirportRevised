namespace Airport.Models.DTOs
{
    public abstract class StationForOperationDTO
    {
        public abstract FlightDTO? Flight { get; set; }
        public abstract TimeSpan WaitingTime { get; set; }
    }
}

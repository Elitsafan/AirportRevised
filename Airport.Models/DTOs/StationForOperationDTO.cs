namespace Airport.Models.DTOs
{
    public abstract class StationForOperationDTO
    {
        public abstract TimeSpan EstimatedWaitingTime { get; set; }
    }
}

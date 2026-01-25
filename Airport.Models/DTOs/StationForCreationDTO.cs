namespace Airport.Models.DTOs
{
    public class StationForCreationDTO : StationForOperationDTO
    {
        public override TimeSpan EstimatedWaitingTime { get; set; }
    }
}

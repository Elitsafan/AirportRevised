namespace Airport.Models.DTOs
{
    public class StationForUpdateDTO : StationForOperationDTO
    {
        public override TimeSpan EstimatedWaitingTime { get; set; }
    }
}

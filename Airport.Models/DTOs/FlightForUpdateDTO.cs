using Airport.Models.Entities;
using MongoDB.Bson;

namespace Airport.Models.DTOs
{
    public class FlightForUpdateDTO
    {
        private List<OccupationDetails>? _stationOccupationDetails;
        public List<OccupationDetails> StationOccupationDetails
        {
            get
            {
                _stationOccupationDetails ??= new();
                return _stationOccupationDetails;
            }
            set => _stationOccupationDetails = value;
        }
        public ObjectId? RouteId { get; set; }
    }
}

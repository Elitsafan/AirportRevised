using MongoDB.Bson;
using Airport.Models.Entities;

namespace Airport.Models.DTOs
{
    public class FlightUpdateDTO
    {
        private List<OccupationDetails>? _stationOccupationDetails;
        public List<OccupationDetails> StationOccupationDetails
        {
            get
            {
                _stationOccupationDetails ??= new List<OccupationDetails>();
                return _stationOccupationDetails;
            }
            set => _stationOccupationDetails = value;
        }
        public ObjectId? RouteId { get; set; }
    }
}

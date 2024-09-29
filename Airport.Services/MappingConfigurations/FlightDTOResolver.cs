using Airport.Contracts.Logics;
using Airport.Models.DTOs;
using Airport.Models.Enums;
using AutoMapper;

namespace Airport.Services.MappingConfigurations
{
    internal class FlightDTOResolver : IValueResolver<IStationLogic, StationDTO, FlightDTO?>
    {
        public FlightDTO? Resolve(
            IStationLogic source,
            StationDTO destination,
            FlightDTO? destMember,
            ResolutionContext context) => !source.CurrentFlightType.HasValue
                ? null
                : source.CurrentFlightType! == FlightType.Landing
                    ? new LandingDTO { FlightId = source.CurrentFlightId!.Value }
                    : new DepartureDTO { FlightId = source.CurrentFlightId!.Value };
    }
}

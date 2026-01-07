using Airport.Contracts.Logics;
using Airport.Models.DTOs;
using Airport.Models.Entities;
using Airport.Models.Enums;
using AutoMapper;

namespace Airport.Services.MappingConfigurations
{
    public class StationProfile : Profile
    {
        public StationProfile()
        {
            CreateMap<Station, StationDTO>();
            CreateMap<IStationLogic, StationDTO>()
                .ForMember(dest => dest.StationId, opt => opt.MapFrom(src => src.StationId))
                .ForMember(dest => dest.WaitingTime, opt => opt.MapFrom(src => src.EstimatedWaitingTime))
                .ForMember(dest => dest.Flight, opt => opt.MapFrom(src => CreateFlightDTO(src)));
        }

        private FlightDTO? CreateFlightDTO(IStationLogic source) => !source.CurrentFlightType.HasValue || !source.CurrentFlightId.HasValue
            ? null
            : source.CurrentFlightType.Value switch
            {
                FlightType.Departure => new DepartureDTO { FlightId = source.CurrentFlightId.Value },
                FlightType.Landing => new LandingDTO { FlightId = source.CurrentFlightId.Value },
                _ => null
            };
    }
}

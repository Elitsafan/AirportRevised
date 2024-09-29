using AutoMapper;
using Airport.Models.DTOs;
using Airport.Models.Entities;
using Airport.Models.Enums;

namespace Airport.Services.MappingConfigurations
{
    public class FlightProfile : Profile
    {
        public FlightProfile()
        {
            CreateMap<LandingForCreationDTO, Flight>()
                .ConstructUsing(dto => new Landing());
            CreateMap<DepartureForCreationDTO, Flight>()
                .ConstructUsing(dto => new Departure());
            CreateMap<Landing, LandingDTO>();
            CreateMap<Departure, DepartureDTO>();
            CreateMap<Flight, FlightDTO>()
                .ConstructUsing(f => f.ConvertToFlightType() == FlightType.Landing
                    ? new LandingDTO()
                    : new DepartureDTO());
        }
    }
}

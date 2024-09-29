using Airport.Contracts.Logics;
using Airport.Models.DTOs;
using Airport.Models.Entities;
using AutoMapper;

namespace Airport.Services.MappingConfigurations
{
    public class StationProfile : Profile
    {
        public StationProfile()
        {
            CreateMap<Station, StationDTO>();
            CreateMap<IStationLogic, StationDTO>()
                .ForMember(
                    dest => dest.Flight,
                    opt => opt.MapFrom<FlightDTOResolver>());
        }
    }
}

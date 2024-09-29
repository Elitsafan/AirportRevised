using AutoMapper;
using Airport.Models.DTOs;
using Airport.Models.Entities;

namespace Airport.Services.MappingConfigurations
{
    public class DirectionProfile : Profile
    {
        public DirectionProfile()
        {
            CreateMap<Direction, DirectionDTO>();
        }
    }
}

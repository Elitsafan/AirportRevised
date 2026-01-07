using AutoMapper;
using Airport.Models.DTOs;
using Airport.Models.Entities;

namespace Airport.Services.MappingConfigurations
{
    public class RouteProfile : Profile
    {
        public RouteProfile()
        {
            CreateMap<Route, RouteDTO>();
        }
    }
}

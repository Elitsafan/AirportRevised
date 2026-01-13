using Airport.Models.DTOs;
using Airport.Models.Entities;
using AutoMapper;

namespace Airport.Services.MappingConfigurations
{
    public class RouteProfile : Profile
    {
        public RouteProfile()
        {
            CreateMap<Route, RouteDTO>();
            CreateMap<RouteDTO, Route>();
            CreateMap<RouteForCreationDTO, RouteDTO>();
            CreateMap<RouteForUpdateDTO, Route>();
        }
    }
}

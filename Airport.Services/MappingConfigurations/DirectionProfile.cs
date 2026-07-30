namespace Airport.Services.MappingConfigurations
{
    public class DirectionProfile : Profile
    {
        public DirectionProfile()
        {
            CreateMap<Direction, DirectionDTO>();
            CreateMap<DirectionDTO, Direction>();
        }
    }
}

namespace Airport.Services.MappingConfigurations
{
    public class SectionProfile : Profile
    {
        public SectionProfile() => CreateMap<SectionDTO<ObjectId>, Section>()
            .ForMember(dest => dest.SectionId, opt => ObjectId.GenerateNewId());
    }
}

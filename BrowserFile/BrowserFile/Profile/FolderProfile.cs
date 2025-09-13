using BrowserFile.Models.DTO;
using BrowserFile.Models.Entities;

namespace BrowserFile.Profile
{
    public class FolderProfile : AutoMapper.Profile
    {
        public FolderProfile()
        {
            CreateMap<FolderDTO,Folder>()
                .ForMember(dest => dest.IconId, opt => opt.MapFrom(src => src.IconId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Tag, opt => opt.MapFrom(src => src.Tag))
                .ReverseMap();

            CreateMap<FolderShortModelDTO,Folder>()
                .ReverseMap();
        }
    }
}

using AutoMapper;
using Cola.DTO;
using Cola.Model;
using System.Data;

namespace Cola.Extensions
{
    public class CustomProfile : Profile
    {
        public CustomProfile()
        {
            CreateMap<CheckItem, CheckItemDTO>()
            .ForMember(a => a.Name, o => o.MapFrom(d => d.alias_name));
        }

    }
}

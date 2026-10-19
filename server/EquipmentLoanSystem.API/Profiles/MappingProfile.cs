using AutoMapper;
using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Entities;
using EquipmentLoanSystem.Core.Enums;

namespace EquipmentLoanSystem.API.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ---------- Entities -> Response DTOs ----------
            CreateMap<Item, ItemDto>()
                .ForMember(d => d.Categories, o => o.MapFrom(s => s.Categories.Select(c => c.Name).OrderBy(n => n).ToList()))
                .ForMember(d => d.TotalCopies, o => o.MapFrom(s => s.Copies.Count))
                .ForMember(d => d.AvailableCopies, o => o.MapFrom(s => s.Copies.Count(c => c.Status == CopyStatus.Available)));

            CreateMap<Category, CategoryDto>();

            CreateMap<Loan, LoanDto>()
                .ForMember(d => d.ItemId, o => o.MapFrom(s => s.ItemCopy.ItemId))
                .ForMember(d => d.ItemName, o => o.MapFrom(s => s.ItemCopy.Item != null ? s.ItemCopy.Item.Name : string.Empty))
                .ForMember(d => d.SerialNumber, o => o.MapFrom(s => s.ItemCopy.SerialNumber))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.Message, o => o.Ignore());

            // ---------- Request DTOs -> Entities ----------
            CreateMap<CreateItemDto, Item>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name.Trim()))
                .ForMember(d => d.Description, o => o.MapFrom(s => (s.Description ?? string.Empty).Trim()))
                .ForMember(d => d.Categories, o => o.Ignore())
                .ForMember(d => d.Copies, o => o.Ignore());
        }
    }
}

using AutoMapper;
using CustomerService.Coommands.RelationshipCommands;
using CustomerService.Domains.Customers;
using CustomerService.DTOs.RelationshipDTOs;

namespace CustomerService.Profiles.Relationships;

public class RelationshipProfile : Profile
{
    public RelationshipProfile()
    {
        CreateMap<CreateRelationshipDTO, Relationship>().ReverseMap();
        CreateMap<ReadRelationshipDTO, Relationship>().ReverseMap();
        CreateMap<CreateRelationshipCommands, Relationship>().ReverseMap();
    }
}

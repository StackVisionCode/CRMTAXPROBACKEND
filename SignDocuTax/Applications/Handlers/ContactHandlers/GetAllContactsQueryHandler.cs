using AutoMapper;
using Common;
using DTOs.Contacts;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.Contacts;
using Queries.Documents;

namespace Handlers.ContactHandlers;


public class GetAllContactsQueryHandler : IRequestHandler<GetAllContactsQuery, ApiResponse<List<ContactDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAllContactsQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }



    public async Task<ApiResponse<List<ContactDto>>> Handle(GetAllContactsQuery request, CancellationToken cancellationToken)
    {
        var contacts = await _context.Contacts.ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<ContactDto>>(contacts);
        return new ApiResponse<List<ContactDto>>(true, "Contact retrieved successfully", dtos);
      
    }
}


using AutoMapper;
using Common;
using DTOs.Contacts;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.Contacts;

public class GetContactsByNameQueryHandler : IRequestHandler<GetContactsByNameQuery, ApiResponse<List<ContactDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetContactsByNameQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<ContactDto>>> Handle(GetContactsByNameQuery request, CancellationToken cancellationToken)
    {
        var contacts = await _context.Contacts
            .Where(x => x.Name.Contains(request.Name))
            .ToListAsync(cancellationToken);

        return new ApiResponse <List<ContactDto>>(true,"Contact obteneid",_mapper.Map<List<ContactDto>>(contacts));
    }
}

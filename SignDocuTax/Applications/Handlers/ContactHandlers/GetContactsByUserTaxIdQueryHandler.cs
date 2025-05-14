using AutoMapper;
using Common;
using DTOs.Contacts;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.Contacts;

public class GetContactsByUserTaxIdQueryHandler : IRequestHandler<GetContactsByUserTaxIdQuery, ApiResponse<List<ContactDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetContactsByUserTaxIdQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<ContactDto>>> Handle(GetContactsByUserTaxIdQuery request, CancellationToken cancellationToken)
    {
        var contacts = await _context.Contacts
            .Where(x => x.UserTaxId == request.UserTaxId)
            .ToListAsync(cancellationToken);

        return new ApiResponse<List<ContactDto>>(true,"Contacts obteneid",_mapper.Map<List<ContactDto>>(contacts));
    }
}

using Common;
using DTOs.Contacts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Infraestructure.Context;
using Queries.Contacts;

namespace Handlers.Contacts
{
    public class GetContactByIdQueryHandler : IRequestHandler<GetContactByIdQuery, ApiResponse<ContactDto>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetContactByIdQueryHandler(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<ContactDto>> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
        {
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (contact == null)
                return new ApiResponse<ContactDto>(false, "Contact no encontrado.");

            return new ApiResponse<ContactDto>(true, "Contact obtenid", _mapper.Map<ContactDto>(contact));
        }
    }
}

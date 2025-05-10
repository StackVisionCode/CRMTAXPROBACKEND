using Common;
using DTOs.Contacts;
using MediatR;

namespace Queries.Contacts
{
    public record class GetContactByIdQuery(int Id) : IRequest<ApiResponse<ContactDto>>;
}

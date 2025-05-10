using Common;
using DTOs.Contacts;
using MediatR;

namespace Queries.Contacts
{
    public record class GetContactsByUserTaxIdQuery(int UserTaxId) : IRequest<ApiResponse<List<ContactDto>>>;
}

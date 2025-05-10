using Common;
using DTOs.Contacts;
using MediatR;

namespace Queries.Contacts
{
    public record class GetContactsByNameQuery(string Name) : IRequest<ApiResponse<List<ContactDto>>>;
}

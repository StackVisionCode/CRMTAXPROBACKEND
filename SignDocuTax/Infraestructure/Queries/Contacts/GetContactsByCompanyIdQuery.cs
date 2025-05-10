using Common;
using DTOs.Contacts;
using MediatR;

namespace Queries.Contacts
{
    public record class GetContactsByCompanyIdQuery(int CompanyId) : IRequest<ApiResponse<List<ContactDto>>>;
}

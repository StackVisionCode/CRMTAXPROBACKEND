using Common;
using DTOs.Contacts;
using MediatR;

namespace Queries.Contacts;

public record class GetAllContactsQuery : IRequest<ApiResponse<List<ContactDto>>>;



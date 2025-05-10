using Common;
using MediatR;

namespace Commands.Contacts
{
    public  record  class DeleteContactCommand(int Id) : IRequest<ApiResponse<bool>>;
}

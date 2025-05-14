using Commands.Contacts;
using Common;
using Infraestructure.Context;
using MediatR;


namespace Handlers.Contacts
{
    public class DeleteContactCommandHandler : IRequestHandler<DeleteContactCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _context;

        public DeleteContactCommandHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
        {
            var contact = await _context.Contacts.FindAsync(request.Id);

            if (contact == null)
                return new ApiResponse<bool>(false, "Error no found contact");

            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync(cancellationToken);

            return new ApiResponse<bool>(true, "Contact deleted succesfully");
        }
    }
}

using AutoMapper;
using Commands.Contacts;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.ContactHandlers;

public class UpdateContactCommandHandler : IRequestHandler<UpdateContactCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateContactCommandHandler> _logger;

    public UpdateContactCommandHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<UpdateContactCommandHandler> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }



    public async Task<ApiResponse<bool>> Handle(UpdateContactCommand request, CancellationToken cancellationToken)
    {
        try
        {
              // 2. Verificar si el email ya existe
        if (await EmailExists(request.UpdateContactDto.Email, cancellationToken))
        {
            return new ApiResponse<bool>(false, "El email ya está registrado");
        }
            var contact = await _dbContext.Contacts.FirstOrDefaultAsync(x => x.Id == request.UpdateContactDto.Id, cancellationToken);
            if (contact == null)
                return new ApiResponse<bool>(false, "Contact not found");
            _mapper.Map(request.UpdateContactDto, contact);
            contact.UpdatedAt = DateTime.UtcNow;

            _dbContext.Contacts.Update(contact);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            _logger.LogInformation("contact updated successfully: {@Contact}", contact);
            return new ApiResponse<bool>(result, result ? "Contact updated successfully" : "Failed to update contact", result);
        }
        catch (Exception ex)
        {

            _logger.LogError("Error update contact: {@Contact}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message);

        }
    }

     private async Task<bool> EmailExists(string email, CancellationToken cancellationToken)
    {
        return await _dbContext.Contacts
            .AnyAsync(c => c.Email.ToLower() == email.ToLower(), cancellationToken);
    }
}


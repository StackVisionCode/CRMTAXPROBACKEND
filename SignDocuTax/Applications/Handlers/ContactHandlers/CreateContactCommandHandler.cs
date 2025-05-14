using AutoMapper;
using Commands.Contacts;
using Common;
using Domains.Contacts;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Handlers.ContactHandlers;

public class CreateContactCommandHandler : IRequestHandler<CreateContactCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateContactCommandHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<bool>> Handle(CreateContactCommand request, CancellationToken cancellationToken)
    {

        if (!IsValidEmailFormat(request.CreateContactDto.Email))
        {
            return new ApiResponse<bool>(false, "El formato del email no es válido");
        }

        // 2. Verificar si el email ya existe
        if (await EmailExists(request.CreateContactDto.Email, cancellationToken))
        {
            return new ApiResponse<bool>(false, "El email ya está registrado");
        }
        var contact = _mapper.Map<Contact>(request.CreateContactDto);
        contact.CreatedAt = DateTime.UtcNow;
        contact.UpdatedAt = DateTime.UtcNow;
        _context.Contacts.Add(contact);
        var results = await _context.SaveChangesAsync(cancellationToken) > 0;
        return new ApiResponse<bool>(results, results ? "Document created successfully" : "Failed to create document", results);
    }
    private bool IsValidEmailFormat(string email)
    {
        try
        {
            var mailAddress = new System.Net.Mail.MailAddress(email);
            return mailAddress.Address == email;
        }
        catch
        {
            return false;
        }
    }
    private async Task<bool> EmailExists(string email, CancellationToken cancellationToken)
    {
        return await _context.Contacts
            .AnyAsync(c => c.Email.ToLower() == email.ToLower(), cancellationToken);
    }

}


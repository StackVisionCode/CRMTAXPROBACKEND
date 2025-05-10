using Domain.Signatures;
using Infraestructure.Context;
using Microsoft.EntityFrameworkCore;
using Services.Contracts;

namespace Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IInternalMessagingService _messagingService;

    public NotificationService(
        ApplicationDbContext context,
        IEmailService emailService,
        IInternalMessagingService messagingService)
    {
        _context = context;
        _emailService = emailService;
        _messagingService = messagingService;
    }

    public async Task NotifySignersAsync(int requirementId, string? customMessage = null)
    {
        var requirement = await _context.RequirementSignatures
            .Include(r => r.EventSignatures)
            .Include(r => r.ExternalSigners)
            .ThenInclude(e => e.Contact)
            .FirstOrDefaultAsync(r => r.Id == requirementId);

        if (requirement == null) return;

        

        // Notificar firmantes externos
        foreach (var e in requirement.ExternalSigners.Where(e => e.SignedDate == null))
        {
            var signingUrl = $"/sign-external?token={e.SigningToken}";
            await _emailService.SendAsync(e.Contact.Email, 
                "Solicitud de firma", 
                customMessage ?? $"Por favor firme el documento: {signingUrl}");
        }
    }

    public Task SendEmailAsync(string email, string subject, string message)
    {
        return _emailService.SendAsync(email, subject, message);
    }

    
}
using Common;
using Infraestructure.Context;
using Microsoft.EntityFrameworkCore;
using Services.Contracts;

namespace Services;

public class InternalMessagingService : IInternalMessagingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InternalMessagingService> _logger;


    public InternalMessagingService(
        ApplicationDbContext context,
        ILogger<InternalMessagingService> logger)
    {
        _context = context;
        _logger = logger;

    }


   Task IInternalMessagingService.SendAsync(int userId, string title, string message)
    {
            
       return null;
    }
}
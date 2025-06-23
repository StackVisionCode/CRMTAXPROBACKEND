using CommLinkServices.Domain;
using CommLinkServices.Infrastructure.Commands;
using CommLinkServices.Infrastructure.Context;
using Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkServices.Application.Handlers;

public class EnsureConversationHandler
    : IRequestHandler<EnsureConversationCommand, ApiResponse<Guid>>
{
    private readonly ILogger<EnsureConversationHandler> _logger;
    private readonly CommLinkDbContext _context;

    public EnsureConversationHandler(
        ILogger<EnsureConversationHandler> logger,
        CommLinkDbContext context
    )
    {
        _logger = logger;
        _context = context;
    }

    public async Task<ApiResponse<Guid>> Handle(
        EnsureConversationCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var conve = await _context
                .Conversations.AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        (
                            x.FirstUserId == request.RequesterId
                            && x.SecondUserId == request.OtherUserId
                        )
                        || (
                            x.FirstUserId == request.OtherUserId
                            && x.SecondUserId == request.RequesterId
                        ),
                    cancellationToken
                );

            if (conve == null)
            {
                conve = new Conversation
                {
                    Id = Guid.NewGuid(),
                    FirstUserId = request.RequesterId,
                    SecondUserId = request.OtherUserId,
                    CreatedAt = DateTime.UtcNow,
                };
                _context.Conversations.Add(conve);
                var result = await _context.SaveChangesAsync(cancellationToken) > 0;
                _logger.LogInformation("Conversation created successfully: {Conversation}", conve);

                return new ApiResponse<Guid>(result, "Conversation created successfully", conve.Id);
            }

            return new ApiResponse<Guid>(true, "Conversation already exists", conve.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role: {Message}", ex.Message);
            return new ApiResponse<Guid>(false, ex.Message, Guid.Empty);
        }
    }
}

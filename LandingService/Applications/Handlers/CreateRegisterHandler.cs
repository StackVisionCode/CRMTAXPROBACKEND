using AutoMapper;
using Common;
using LandingService.Domain;
using LandingService.Infrastructure.Commands;
using LandingService.Infrastructure.Context;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.AuthEvents;

namespace LandingService.Applications.Handlers;

public class CreateRegisterHandler : IRequestHandler<CreateRegisterCommands, ApiResponse<bool>>
{
    private readonly ILogger<CreateRegisterHandler> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;


    public CreateRegisterHandler(ILogger<CreateRegisterHandler> logger, ApplicationDbContext context, IMapper mapper, IEventBus eventBus)
    {
        _logger = logger;
        _context = context;
        _mapper = mapper;
        _eventBus = eventBus;
    }
    public async Task<ApiResponse<bool>> Handle(CreateRegisterCommands request, CancellationToken cancellationToken)
    {

        try
        {
            var entityMap = _mapper.Map<User>(request.requestDto);

            await _context.Users.AddAsync(entityMap, cancellationToken);
            var result = await _context.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                return new ApiResponse<bool> { Success = false, Message = "An error occurred while creating the record.", Data = false };

            }
            _eventBus.Publish(
             new AccountConfirmationLinkEvent(
                 Id: Guid.NewGuid(),
                 OccurredOn: DateTime.UtcNow,
                 UserId:entityMap.Id,
                 Email: entityMap.Email,
                 DisplayName: "",
                 ConfirmLink: "https://taxproshield.com/confirm?email=" + entityMap.Email + "&token=" + entityMap.ConfirmToken,
                 ExpiresAt: DateTime.UtcNow.AddHours(24), // Configurar según necesidades
                 CompanyId: entityMap.Id,
                 IsCompany: false,
                 CompanyFullName: entityMap.CompanyName,
                 CompanyName: entityMap.CompanyName,
                 AdminName: entityMap.Email,
                 Domain: ""
             )
         );
            return new ApiResponse<bool> { Success = result ? true : false, Message = result ? "Register Successfully" : "An error occurred while creating the record", Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el registro");
            return new ApiResponse<bool> { Success = false, Message = "An error occurred while creating the record.", Data = false };
        }

    }
}
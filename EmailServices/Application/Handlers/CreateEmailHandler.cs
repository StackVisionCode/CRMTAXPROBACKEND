using Application.Common.DTO;
using AutoMapper;
using Common;
using Infrastructure.Commands;
using Infrastructure.Context;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Application.Handlers;

public class CreateEmailHandler : IRequestHandler<CreateEmailCommands, ApiResponse<bool>>
{
    private readonly ILogger<CreateEmailHandler> _logger;
    private readonly EmailContext _context;
    private readonly IMapper _map;
    private readonly IEmail _email;
    public CreateEmailHandler(ILogger<CreateEmailHandler> logger, EmailContext context, IMapper map, IConfiguration confiEmail, IEmail email)
    {
        _logger = logger;
        _context = context;
        _map = map;       
        _email = email;
    }


    public async Task<ApiResponse<bool>> Handle(CreateEmailCommands request, CancellationToken cancellationToken)
    {
       try
       {          
                if (request is null)
                {
                    return new ApiResponse<bool>(false,"",false);
                }
             var result =   request.Deconstruct;

                var EmailResponse = await _email.SendEmail(result.)
       }
       catch (System.Exception)
       {
        
        throw;
       }
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AutoMapper;
using Common;
using LandingService.Infrastructure.Commands;
using LandingService.Infrastructure.Context;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace LandingService.Applications.Handlers;

public class CreateConfirmEmailHandler : IRequestHandler<CreateConfirmEmail, ApiResponse<string>>
{
    private readonly ILogger<CreateConfirmEmailHandler> _Loger;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _jwtSettings;
    private readonly IEventBus _eventBus;
    private readonly IMapper _mapper;
    public CreateConfirmEmailHandler(ILogger<CreateConfirmEmailHandler> loger, ApplicationDbContext context, IConfiguration jwtSettings, IEventBus eventBus, IMapper mapper)
    {
        _Loger = loger;
        _context = context;
        _jwtSettings = jwtSettings;
        _eventBus = eventBus;
        _mapper = mapper;
    }
    public async Task<ApiResponse<string>> Handle(CreateConfirmEmail request, CancellationToken cancellationToken)
    {
        try
        {
            var resultkey = _jwtSettings.GetSection("JwtSettings").Get<JwtSettings>();
            if (resultkey == null)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "JWT settings are not configured properly.",
                    Data = null
                };
            }
            var data = await _context.Users.FindAsync(request.request.Email);
            if (data == null)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "User not found.",
                    Data = null
                };
            }
            if (data.Confirm == false)
            {

                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Email confirmation is pending.",
                    Data = "Email confirmation is pending."
                };
            }
            if (data.ConfirmToken != request.request.Token)
                return new ApiResponse<string>(false, "Invalid token", "Invalid token");

            var handler = new JwtSecurityTokenHandler();
            var prm = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(resultkey.SecretKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
            };
            var result = handler.ValidateToken(request.request.Token, prm, out _);
            if (result == null)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Token validation failed.",
                    Data = null
                };
            }

            data.Confirm = true;
            data.IsActive = true;
            data.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);


            _eventBus.Publish(
                new AccountConfirmedEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    data.Id,
                    data.Name,
                    data.LastName,
                    $"{data.Name} {data.LastName}",
                    data.CompanyName,
                    "",
                    false,
                   data.Id,
                    data.Email
                )
            );

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Email confirmed successfully.",
                Data = "Email confirmed successfully."
            };


        }
        catch (Exception ex)
        {

            _Loger.LogError(ex, "Error in CreateConfirmEmailHandler");
            return new ApiResponse<string>
            {
                Success = false,
                Message = "An error occurred while processing your request.",
                Data = null
            };

        }
    }
}

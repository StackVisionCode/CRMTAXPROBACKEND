using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.ContactInfoQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.ContactInfoHandlers;

public class GetAuthInfoByEmailHandler
    : IRequestHandler<GetAuthInfoByEmailQuery, ApiResponse<AuthInfoDTO>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GetAuthInfoByEmailHandler> _logger;

    public GetAuthInfoByEmailHandler(
        ApplicationDbContext db,
        ILogger<GetAuthInfoByEmailHandler> logger
    )
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthInfoDTO>> Handle(
        GetAuthInfoByEmailQuery req,
        CancellationToken ct
    )
    {
        try
        {
            var result = await (
                from c in _db.Customers
                join ci in _db.ContactInfos on c.Id equals ci.CustomerId
                where ci.Email == req.Email
                select new AuthInfoDTO
                {
                    CustomerId = c.Id,
                    Email = ci.Email,
                    PasswordHash = ci.PasswordClient!,
                    IsLogin = ci.IsLoggin,
                    DisplayName = (c.FirstName + " " + c.LastName).Trim(),
                }
            ).FirstOrDefaultAsync(ct);

            if (result is null)
                return new ApiResponse<AuthInfoDTO>(false, "No encontrado");

            return new ApiResponse<AuthInfoDTO>(true, "Ok", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new ApiResponse<AuthInfoDTO>(false, ex.Message);
        }
    }
}

using System.Security.Claims;
using CommLinkService.Domain.Entities;

namespace CommLinkService.Application.Common.Shared;

public static class UserTokenHelper
{
    public static UserInfo? GetUserFromToken(ClaimsPrincipal user)
    {
        var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var companyIdStr = user.FindFirst("companyId")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return null;

        Guid? companyId = null;
        if (
            !string.IsNullOrEmpty(companyIdStr)
            && Guid.TryParse(companyIdStr, out var tempCompanyId)
        )
            companyId = tempCompanyId;

        // Determinar tipo de usuario basado en CompanyId
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            return new UserInfo
            {
                UserType = ParticipantType.TaxUser,
                TaxUserId = userId,
                CustomerId = null,
                CompanyId = companyId,
            };
        }
        else
        {
            return new UserInfo
            {
                UserType = ParticipantType.Customer,
                TaxUserId = null,
                CustomerId = userId,
                CompanyId = null,
            };
        }
    }
}

public class UserInfo
{
    public ParticipantType UserType { get; set; }
    public Guid? TaxUserId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CompanyId { get; set; }
}

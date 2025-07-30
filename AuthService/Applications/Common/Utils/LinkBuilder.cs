using Microsoft.AspNetCore.Http;

namespace Applications.Common;

public class LinkBuilder
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LinkBuilder(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string BuildConfirmationLink(string? origin, string email, string token)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // ➊ Si el header vino vacío, usa la URL del request
        var baseUrl = string.IsNullOrWhiteSpace(origin)
            ? $"{httpContext?.Request.Scheme ?? "https"}://{httpContext?.Request.Host.Value}"
            : origin.TrimEnd('/');

        return $"{baseUrl}/auth/confirm-account"
            + $"?email={Uri.EscapeDataString(email)}"
            + $"&token={Uri.EscapeDataString(token)}";
    }
}

using Microsoft.AspNetCore.Http;

namespace SharedLibrary.Helpers;

public class GetOriginURL
{
    private readonly IHttpContextAccessor _ctx;

    public GetOriginURL(IHttpContextAccessor ctx)
    {
        _ctx = ctx;
    }

    public string GetOrigin()
    {
        var hdr = _ctx.HttpContext?.Request.Headers["X-Front-Base-Url"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(hdr))
            return hdr!.TrimEnd('/');

        var origin = _ctx.HttpContext?.Request.Headers["Origin"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(origin))
            return origin!.TrimEnd('/');

        var req = _ctx.HttpContext!.Request;
        return $"{req.Scheme}://{req.Host}".TrimEnd('/');
    }
}

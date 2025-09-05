using System.Net;

namespace AuthService.Applications.Common.Utils;

public static class IpAddressHelper
{
    public static string GetClientIp(HttpContext ctx)
    {
        // 1) Cloudflare
        var cfIp = ctx.Request.Headers["CF-Connecting-IP"].ToString();
        if (IsValidPublicIp(cfIp))
            return cfIp;

        // 1.5) X-Real-IP (ingress alternos)
        var xri = ctx.Request.Headers["X-Real-IP"].ToString();
        if (IsValidPublicIp(xri))
            return xri;

        // 2) X-Forwarded-For (primer IP público)
        var xff = ctx.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(xff))
        {
            foreach (var raw in xff.Split(','))
            {
                var ip = raw.Trim();
                if (IsValidPublicIp(ip))
                    return ip;
            }
        }

        // 3) Fallback: conexión (ya ‘reparada’ por UseForwardedHeaders)
        return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool IsValidPublicIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;
        if (!IPAddress.TryParse(ip, out var address))
            return false;

        // IPv4 privadas o reservadas
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return false;
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return false;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return false;
            // 127.0.0.0/8 (loopback)
            if (bytes[0] == 127)
                return false;
            // 169.254.0.0/16 (link-local)
            if (bytes[0] == 169 && bytes[1] == 254)
                return false;
            // 100.64.0.0/10 (CGNAT)
            if (bytes[0] == 100 && (bytes[1] >= 64 && bytes[1] <= 127))
                return false;
        }
        else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (IPAddress.IsLoopback(address))
                return false;
            // fe80::/10 link-local, fc00::/7 unique local
            var s = address.ToString().ToLowerInvariant();
            if (s.StartsWith("fe80:") || s.StartsWith("fc") || s.StartsWith("fd"))
                return false;
        }

        return true;
    }
}

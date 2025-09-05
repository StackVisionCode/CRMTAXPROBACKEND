using System.Net;
using System.Text.RegularExpressions;

namespace CommLinkService.Application.Common.Utils;

public static class IpAddressHelper
{
    public static string GetClientIp(HttpContext ctx)
    {
        // Cloudflare
        var cf = ctx.Request.Headers["CF-Connecting-IP"].ToString();
        if (IsValidPublicIp(cf))
            return cf;

        // X-Real-IP
        var xri = ctx.Request.Headers["X-Real-IP"].ToString();
        if (IsValidPublicIp(xri))
            return xri;

        // RFC 7239: Forwarded: for=1.2.3.4, for="[2001:db8:cafe::17]"
        var fwd = ctx.Request.Headers["Forwarded"].ToString();
        if (!string.IsNullOrWhiteSpace(fwd))
        {
            var ips = ParseForwardedFor(fwd);
            foreach (var ip in ips)
                if (IsValidPublicIp(ip))
                    return ip;
        }

        // X-Forwarded-For
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

        return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static IEnumerable<string> ParseForwardedFor(string header)
    {
        // busca for=... (permite IPv6 entre [])
        // Ej: for=192.0.2.60;proto=http;by=203.0.113.43, for="[2001:db8:cafe::17]"
        var list = new List<string>();
        var parts = header.Split(',');
        foreach (var part in parts)
        {
            var m = Regex.Match(
                part,
                @"for=\s*(?<ip>""?\[?[A-Fa-f0-9\.\:]+?\]?""?)",
                RegexOptions.IgnoreCase
            );
            if (m.Success)
            {
                var raw = m.Groups["ip"].Value.Trim().Trim('"');
                if (raw.StartsWith("[") && raw.EndsWith("]"))
                    raw = raw[1..^1]; // IPv6 brackets
                list.Add(raw);
            }
        }
        return list;
    }

    private static bool IsValidPublicIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;
        if (!IPAddress.TryParse(ip, out var address))
            return false;

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            if (b[0] == 10)
                return false; // 10.0.0.0/8
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                return false; // 172.16/12
            if (b[0] == 192 && b[1] == 168)
                return false; // 192.168/16
            if (b[0] == 127)
                return false; // loopback
            if (b[0] == 169 && b[1] == 254)
                return false; // link-local
            if (b[0] == 100 && (b[1] >= 64 && b[1] <= 127))
                return false; // CGNAT 100.64/10
        }
        else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (IPAddress.IsLoopback(address))
                return false;
            var s = address.ToString().ToLowerInvariant();
            if (s.StartsWith("fe80:") || s.StartsWith("fc") || s.StartsWith("fd"))
                return false; // link/unique local
        }

        return true;
    }
}

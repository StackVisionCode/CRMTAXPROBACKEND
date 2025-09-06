using System.Security.Cryptography;
using System.Text;
using CommLinkService.Application.DTOs.VideoCallDTOs;
using CommLinkService.Infrastructure.Configuration;
using CommLinkService.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace CommLinkService.Services
{
    public class WebRTCService : IWebRTCService
    {
        private readonly TurnServerConfig _turnConfig;
        private readonly ILogger<WebRTCService> _logger;

        public WebRTCService(IOptions<TurnServerConfig> turnConfig, ILogger<WebRTCService> logger)
        {
            _turnConfig = turnConfig.Value;
            _logger = logger;
        }

        public async Task<TurnCredentialsDto> GenerateTurnCredentialsAsync()
        {
            try
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var expirationTimestamp = timestamp + _turnConfig.CredentialTtlSeconds;
                var username = $"{expirationTimestamp}:webrtc-user";

                var credential = GenerateHmacSha1Credential(username, _turnConfig.SharedSecret);

                var urls = new List<string>
                {
                    $"turn:{_turnConfig.ServerDomain}:{_turnConfig.TurnPort}?transport=tcp",
                    $"turn:{_turnConfig.ServerDomain}:{_turnConfig.TurnPort}?transport=udp",
                };

                if (_turnConfig.EnableTurns)
                {
                    urls.Add(
                        $"turns:{_turnConfig.ServerDomain}:{_turnConfig.TurnsPort}?transport=tcp"
                    );
                }

                var result = new TurnCredentialsDto
                {
                    Username = username,
                    Credential = credential,
                    Urls = urls.ToArray(),
                    TtlSeconds = _turnConfig.CredentialTtlSeconds,
                    ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expirationTimestamp).DateTime,
                };

                _logger.LogInformation(
                    "Generated TURN credentials for user, expires at {ExpiresAt}",
                    result.ExpiresAt
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating TURN credentials");
                throw new InvalidOperationException("Failed to generate TURN credentials", ex);
            }
        }

        public async Task<bool> ValidateTurnServerHealthAsync()
        {
            try
            {
                _logger.LogInformation(
                    "Validating TURN server health for {ServerDomain}",
                    _turnConfig.ServerDomain
                );

                // Aquí podrías hacer un health check real al servidor TURN
                // Por ahora solo validamos configuración
                var isConfigValid =
                    !string.IsNullOrEmpty(_turnConfig.ServerDomain)
                    && !string.IsNullOrEmpty(_turnConfig.SharedSecret)
                    && _turnConfig.TurnPort > 0;

                return await Task.FromResult(isConfigValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating TURN server health");
                return false;
            }
        }

        private string GenerateHmacSha1Credential(string username, string secret)
        {
            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(username));
            return Convert.ToBase64String(hash);
        }
    }
}

using System.Security.AccessControl;

namespace Common;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty; 
    
    public bool ValidateIssuer { get; set; } 
    public bool ValidateAudience { get; set; } 
    public bool ValidateIssuerSigningKey { get; set; }
    public bool ValidateLifetime { get; set; }
    public TimeSpan ClockSkew { get; set; } = TimeSpan.Zero; // Default value for ClockSkew
   
}
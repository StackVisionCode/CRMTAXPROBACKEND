using System.Security.Cryptography;

namespace SharedLibrary.Services;

internal sealed class OtpService : IOtpService
{
    public (string Otp, DateTime Expires) Generate()
    {
        var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var num = BitConverter.ToUInt32(bytes) % 100_000_000;
        return (num.ToString("D8"), DateTime.UtcNow.AddMinutes(5)); // 8-digit OTP, valid for 5 minutes
    }
}

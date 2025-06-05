namespace SharedLibrary.Services;

public interface IOtpService
{
    (string Otp, DateTime Expires) Generate();
}

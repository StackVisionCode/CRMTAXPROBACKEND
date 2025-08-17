namespace SharedLibrary.Contracts;

public interface IInvitationTokenService
{
    /// <summary>
    /// ACTUALIZADO: Genera un token de invitación para TaxUser
    /// </summary>
    (string Token, DateTime Expires) GenerateInvitation(
        Guid companyId,
        string email,
        ICollection<Guid>? roleIds = null
    );

    /// <summary>
    /// Valida un token de invitación y extrae la información
    /// </summary>
    (
        bool IsValid,
        Guid CompanyId,
        string Email,
        ICollection<Guid>? RoleIds,
        string? ErrorMessage
    ) ValidateInvitation(string token);
}

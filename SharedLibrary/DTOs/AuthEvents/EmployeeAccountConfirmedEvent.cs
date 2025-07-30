namespace SharedLibrary.DTOs.AuthEvents;

/// <summary>
/// Evento que se dispara cuando un empleado (no administrador) confirma su cuenta
/// </summary>
public sealed record EmployeeAccountConfirmedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string? Name,
    string? LastName,
    Guid CompanyId,
    string? CompanyFullName,
    string? CompanyName,
    string? CompanyDomain,
    bool IsCompany,
    string? CompanyBrand,
    IEnumerable<string> Roles
) : IntegrationEvent(Id, OccurredOn);

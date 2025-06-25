namespace SharedLibrary.DTOs.CustomerEventsDTO;

/// <summary>Se publica cuando un preparador habilita el login de un cliente.</summary>
public sealed record CustomerLoginEnabledEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid CustomerId,
    string Email,
    string DisplayName,
    string TempPassword // en claro SOLO para el email; NO se almacena
) : IntegrationEvent(Id, OccurredOn);

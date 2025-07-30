namespace SharedLibrary.DTOs.AuthEvents;

public sealed record AddressPayload(
    int CountryId,
    string CountryName,
    int StateId,
    string StateName,
    string? City,
    string? Street,
    string? Line,
    string? ZipCode
);

public sealed record AccountRegisteredEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string Name,
    string LastName,
    string Phone,
    bool IsCompany,
    Guid? CompanyId,
    string? FullName,
    string? CompanyName,
    string? Domain,
    string? Brand,
    AddressPayload? CompanyAddress,
    AddressPayload? UserAddress
) : IntegrationEvent(Id, OccurredOn);

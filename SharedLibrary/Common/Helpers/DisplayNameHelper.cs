namespace SharedLibrary.Common.Helpers;

public static class DisplayNameHelper
{
    public static string From(
        string? name,
        string? lastName,
        string? companyFullName,
        string? companyName,
        bool isCompany,
        string email
    )
    {
        // 1) Si es empresa y tiene CompanyName
        if (isCompany && !string.IsNullOrWhiteSpace(companyName))
            return companyName;

        // 2) Si es individual y tiene FullName
        if (!isCompany && !string.IsNullOrWhiteSpace(companyFullName))
            return companyFullName;

        // 3) Si tiene nombre y apellido del usuario
        if (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(lastName))
            return $"{name} {lastName}".Trim();

        // 4) Fallback al email
        return email;
    }
}

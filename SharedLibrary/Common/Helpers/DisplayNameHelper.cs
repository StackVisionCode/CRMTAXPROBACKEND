namespace SharedLibrary.Common.Helpers;

public static class DisplayNameHelper
{
    public static string From(string? name, string? lastName, string? company, string email)
    {
        // 1) usuario individual
        if (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(lastName))
            return $"{name} {lastName}".Trim();

        // 2) cuenta de compañía
        if (!string.IsNullOrWhiteSpace(company))
            return company;

        // 3) fallback
        return email;
    }
}

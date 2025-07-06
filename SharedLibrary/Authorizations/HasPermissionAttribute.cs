using Microsoft.AspNetCore.Authorization;

namespace SharedLibrary.Authorizations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    private const string POLICY_PREFIX = "PERM_";

    public HasPermissionAttribute(string code)
    {
        Permission = code;
        Policy = POLICY_PREFIX + code; // ej. PERM_TaxUser.Read
    }

    public string Permission { get; }

    public static bool TryParse(string policy, out string code)
    {
        if (policy.StartsWith(POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            code = policy.Substring(POLICY_PREFIX.Length);
            return true;
        }
        code = string.Empty;
        return false;
    }
}

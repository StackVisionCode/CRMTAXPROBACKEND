using Microsoft.AspNetCore.Authorization;

namespace SharedLibrary.Authorizations;

public sealed record PermissionRequirement(string Code) : IAuthorizationRequirement;

using System.Security.Claims;
using Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Identity;

/// <summary>
/// Implementation of ICurrentUserService.
/// Reads user information from JWT claims.
/// </summary>
internal sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Lazy<ClaimsPrincipal?> _user;
    private readonly Lazy<IReadOnlyList<string>> _roles;
    private readonly Lazy<IReadOnlySet<string>> _permissions;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _user = new Lazy<ClaimsPrincipal?>(() => _httpContextAccessor.HttpContext?.User);
        _roles = new Lazy<IReadOnlyList<string>>(GetRolesFromClaims);
        _permissions = new Lazy<IReadOnlySet<string>>(GetPermissionsFromClaims);
    }

    public Guid UserId
    {
        get
        {
            string? claim = _user.Value?.FindFirstValue(CustomClaimTypes.DomainUserId)
                           ?? _user.Value?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(claim, out Guid userId) ? userId : Guid.Empty;
        }
    }

    public Guid IdentityUserId
    {
        get
        {
            string? claim = _user.Value?.FindFirstValue(CustomClaimTypes.IdentityUserId);
            return Guid.TryParse(claim, out Guid userId) ? userId : Guid.Empty;
        }
    }

    public string Email => _user.Value?.FindFirstValue(ClaimTypes.Email)
                           ?? _user.Value?.FindFirstValue("email")
                           ?? string.Empty;

    public bool IsAuthenticated => _user.Value?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles => _roles.Value;

    public IReadOnlySet<string> Permissions => _permissions.Value;

    public bool CanViewSensitiveData
    {
        get
        {
            string? claim = _user.Value?.FindFirstValue(CustomClaimTypes.CanViewSensitiveData);
            return bool.TryParse(claim, out bool canView) && canView;
        }
    }

    public bool IsSuperAdmin => IsInRole("Super Admin");

    public bool IsInRole(string role)
    {
        return _user.Value?.IsInRole(role) ?? false;
    }

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission);
    }

    private List<string> GetRolesFromClaims()
    {
        if (_user.Value is null)
        {
            return [];
        }

        return _user.Value
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
    }

    private HashSet<string> GetPermissionsFromClaims()
    {
        if (_user.Value is null)
        {
            return new HashSet<string>();
        }

        return _user.Value
            .FindAll(CustomClaimTypes.Permissions)
            .Select(c => c.Value)
            .ToHashSet();
    }
}

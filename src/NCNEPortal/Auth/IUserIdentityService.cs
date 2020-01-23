using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NCNEPortal.Auth
{
    public interface IUserIdentityService
    {
        Task<string> GetFullNameForUser(ClaimsPrincipal user);

        Task<bool> ValidateUser(String username);
    }
}
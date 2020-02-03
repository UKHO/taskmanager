using System.Security.Claims;
using System.Threading.Tasks;

namespace Portal.Auth
{
    public interface IUserIdentityService
    {
        Task<string> GetFullNameForUser(ClaimsPrincipal user);
        Task<bool> ValidateUser(string username);
    }
}
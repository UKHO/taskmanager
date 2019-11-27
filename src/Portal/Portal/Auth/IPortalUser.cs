using System.Security.Claims;
using System.Threading.Tasks;

namespace Portal.Auth
{
    public interface IPortalUser
    {
        Task<string> GetFullNameForUser(ClaimsPrincipal user);
    }
}
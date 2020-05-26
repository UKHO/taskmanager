using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowDatabase.EF.Models;

namespace Portal.Auth
{
    public interface IPortalUserDbService
    {
        Task<IEnumerable<AdUser>> GetUsersFromDbAsync();

        Task<bool> ValidateUserAsync(string username);
    }
}
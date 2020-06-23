using DbUpdateWorkflowDatabase.EF.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DbUpdatePortal.Auth
{
    public interface IDbUpdateUserDbService
    {
        Task<IEnumerable<AdUser>> GetUsersFromDbAsync();

        Task<bool> ValidateUserAsync(string username);
    }
}
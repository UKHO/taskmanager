using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NCNEWorkflowDatabase.EF.Models;

namespace NCNEPortal.Auth
{
    public interface INcneUserDbService
    {

        Task<IEnumerable<AdUser>> GetUsersFromDbAsync();

        Task<bool> ValidateUserAsync(string username);

        Task UpdateDbFromAdAsync(IEnumerable<Guid> adGroupGuids);
    }
}
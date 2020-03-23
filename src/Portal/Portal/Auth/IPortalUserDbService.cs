using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowDatabase.EF.Models;

namespace Portal.Auth
{
    public interface IPortalUserDbService
    {

        Task<IEnumerable<AdUser>> GetUsersFromDb();

        Task<bool> ValidateUser(string username);

        Task UpdateDbFromAd(IEnumerable<Guid> adGroupGuids);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers.Auth;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Auth
{
    public class PortalUserDbService : IPortalUserDbService
    {
        private readonly WorkflowDbContext _workflowDbContext;
        private readonly IAdDirectoryService _adDirectoryService;

        public PortalUserDbService(WorkflowDbContext workflowDbContext, IAdDirectoryService adDirectoryService)
        {
            _workflowDbContext = workflowDbContext;
            _adDirectoryService = adDirectoryService;
        }

        /// <summary>
        /// Update users in database from AD
        /// </summary>
        /// <remarks>Bust be awaited from an async function or run with .RunSynchronously()</remarks>
        public async Task UpdateDbFromAdAsync(IEnumerable<Guid> adGroupGuids)
        {

            var adGroupMembers = _adDirectoryService.GetGroupMembersFromAdAsync(adGroupGuids).Result;
            var currentAdUsers = _workflowDbContext.AdUser.ToList();

            var newAdUsers = adGroupMembers.Where(m =>
                currentAdUsers.All(c => c.UserPrincipalName != m.UserPrincipalName)).Select(n => new AdUser
                {
                    DisplayName = n.DisplayName,
                    UserPrincipalName = n.UserPrincipalName,

                    // This will eventually hook into checks
                    // For now, it just records the date user was first stored in our database
                    LastCheckedDate = DateTime.UtcNow
                });

            _workflowDbContext.AdUser.AddRange(newAdUsers);

            try
            {
                await _workflowDbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error saving AD users to database.", e);
            }
        }

        /// <summary>
        /// Get a TM site's users from database
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<AdUser>> GetUsersFromDbAsync()
        {
            return await _workflowDbContext.AdUser.ToListAsync();

        }

        /// <summary>
        /// Validate a username exists in the database
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ValidateUserAsync(string username)
        {
            // TODO do we ever want to go and check AD then update database if we find a new user
            // TODO switch to a unique value instead of display name for validating when we switch over

            return await _workflowDbContext.AdUser.AnyAsync(u => u.DisplayName.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
    }
}

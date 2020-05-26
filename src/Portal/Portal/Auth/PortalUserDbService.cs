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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Helpers.Auth;
using Microsoft.EntityFrameworkCore;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;

namespace NCNEPortal.Auth
{
    public class NcneUserDbService : INcneUserDbService
    {
        private readonly NcneWorkflowDbContext _ncneWorkflowDbContext;
        private readonly IAdDirectoryService _adDirectoryService;

        public NcneUserDbService(NcneWorkflowDbContext ncneWorkflowDbContext, IAdDirectoryService adDirectoryService)
        {
            _ncneWorkflowDbContext = ncneWorkflowDbContext;
            _adDirectoryService = adDirectoryService;
        }

        /// <summary>
        /// Get a TM site's users from database
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<AdUser>> GetUsersFromDbAsync()
        {
            return await _ncneWorkflowDbContext.AdUser.ToListAsync();

        }

        /// <summary>
        /// Validate a username exists in the database
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ValidateUserAsync(string username)
        {
            // TODO do we ever want to go and check AD then update database if we find a new user
            // TODO switch to a unique value instead of display name for validating when we switch over

            return await _ncneWorkflowDbContext.AdUser.AnyAsync(u => u.DisplayName.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
    }
}

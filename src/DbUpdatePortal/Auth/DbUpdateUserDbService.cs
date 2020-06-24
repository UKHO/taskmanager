using Common.Helpers.Auth;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DbUpdatePortal.Auth
{
    public class DbUpdateUserDbService : IDbUpdateUserDbService
    {
        private readonly DbUpdateWorkflowDbContext _dbUpdateWorkflowDbContext;
        private readonly IAdDirectoryService _adDirectoryService;

        public DbUpdateUserDbService(DbUpdateWorkflowDbContext dbUpdateWorkflowDbContext, IAdDirectoryService adDirectoryService)
        {
            _dbUpdateWorkflowDbContext = dbUpdateWorkflowDbContext;
            _adDirectoryService = adDirectoryService;
        }

        /// <summary>
        /// Get a TM site's users from database
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<AdUser>> GetUsersFromDbAsync()
        {
            return await _dbUpdateWorkflowDbContext.AdUser.ToListAsync();

        }

        /// <summary>
        /// Validate a username exists in the database
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ValidateUserAsync(string username)
        {
            // TODO do we ever want to go and check AD then update database if we find a new user
            // TODO switch to a unique value instead of display name for validating when we switch over

            return await _dbUpdateWorkflowDbContext.AdUser.AnyAsync(u => u.DisplayName.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
    }
}

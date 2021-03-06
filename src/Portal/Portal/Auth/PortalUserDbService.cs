﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Auth
{
    public class PortalUserDbService : IPortalUserDbService
    {
        private readonly WorkflowDbContext _workflowDbContext;

        public PortalUserDbService(WorkflowDbContext workflowDbContext)
        {
            _workflowDbContext = workflowDbContext;
        }

        /// <summary>
        /// Get a TM site's users from database
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<AdUser>> GetUsersFromDbAsync()
        {
            return await _workflowDbContext.AdUsers.ToListAsync();
        }

        /// <summary>
        /// Validate a AdUser exists in the database
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ValidateUserAsync(AdUser user)
        {
            return await _workflowDbContext.AdUsers.AnyAsync(u => u.UserPrincipalName == user.UserPrincipalName);
        }

        /// <summary>
        /// Validate a user with UPN exists in the database
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ValidateUserAsync(string userPrincipalName)
        {
            if (string.IsNullOrEmpty(userPrincipalName)) return false;

            return await _workflowDbContext.AdUsers.AnyAsync(u => u.UserPrincipalName == userPrincipalName);
        }

        /// <summary>
        /// Get an AdUsers object for user with given UPN from database
        /// </summary>
        /// <param name="userPrincipalName">Email address uniquely identifying user</param>
        /// <returns></returns>
        public async Task<AdUser> GetAdUserAsync(string userPrincipalName)
        {
            if (!string.IsNullOrEmpty(userPrincipalName))
            {
                try
                {
                    return await _workflowDbContext.AdUsers.SingleAsync(u =>
                         u.UserPrincipalName == userPrincipalName);

                }
                catch (Exception e)
                {
                    throw new ApplicationException($"User with email address {userPrincipalName} not found in database.", e);
                }
            }

            throw new ApplicationException($"Value of {nameof(userPrincipalName)} cannot be null.");
        }
    }
}

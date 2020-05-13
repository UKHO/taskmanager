using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace Common.Helpers.Auth
{
    public interface IAdDirectoryService
    {
        Task<IEnumerable<DirectoryObject>> GetGroupMembersFromAdAsync(Guid groupGuid);

        Task<IEnumerable<(string DisplayName, string UserPrincipalName)>> GetGroupMembersFromAdAsync(
            IEnumerable<Guid> adGroupGuids);

        Task<string> GetFullNameForUserAsync(ClaimsPrincipal user);
    }
}
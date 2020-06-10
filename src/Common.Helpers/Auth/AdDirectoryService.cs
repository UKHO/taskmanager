using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Graph;
using Polly;

namespace Common.Helpers.Auth
{
    public class AdDirectoryService : IAdDirectoryService
    {
        private GraphServiceClient GraphClient { get; }

        public AdDirectoryService(string clientAzureAdSecret, string azureAdClientId, string tenantId,
            Uri landingPageUrl, IHttpProvider httpProvider)
        {
            var redirectUri = string.Concat(landingPageUrl, "signin-oidc");

            var authenticationProvider =
                new MsalAuthenticationProvider(azureAdClientId, clientAzureAdSecret, tenantId, redirectUri);

            GraphClient = new GraphServiceClient(authenticationProvider, httpProvider);

        }

        /// <summary>
        /// Get all AD users in an AD group.
        /// </summary>
        public async Task<IEnumerable<DirectoryObject>> GetGroupMembersFromAdAsync(Guid groupGuid)
        {
            var users = new List<DirectoryObject>();

            // Retries 3 times at 2, 4 and 6 seconds
            var result = await Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(retryAttempt * 2))
                .ExecuteAndCaptureAsync(async () =>
                {
                    users.Clear();
                    var membersPage = await GraphClient.Groups[groupGuid.ToString()].Members.Request().GetAsync().ConfigureAwait(false);
                    users.AddRange(membersPage?.CurrentPage ?? Enumerable.Empty<DirectoryObject>());

                    while (membersPage?.NextPageRequest != null)
                    {
                        membersPage = await membersPage.NextPageRequest.GetAsync().ConfigureAwait(false);
                        users.AddRange(membersPage?.CurrentPage ?? Enumerable.Empty<DirectoryObject>());
                    }
                }).ConfigureAwait(false);

            if (result.Outcome == OutcomeType.Failure)
            {
                throw new ApplicationException($"Failed to connect to Graph API for group: {groupGuid}",
                    result.FinalException);
            }

            if (!users.Any())
            {
                throw new ApplicationException($"No results returned for members of group: {groupGuid}.");
            }

            return users;
        }

        /// <summary>
        /// Get all AD users from a collection of AD groups. 
        /// </summary>
        public async Task<IEnumerable<(string DisplayName, string UserPrincipalName)>> GetGroupMembersFromAdAsync(
            IEnumerable<Guid> adGroupGuids)
        {
            if (adGroupGuids == null) throw new ApplicationException($"{nameof(adGroupGuids)} cannot be null.");

            var users = new List<DirectoryObject>();

            try
            {
                foreach (var groupId in adGroupGuids)
                {
                    users.AddRange(await GetGroupMembersFromAdAsync(groupId).ConfigureAwait(false));
                }

                return users.Select(o => (((User)o).DisplayName, ((User)o).UserPrincipalName))
                    .OrderBy(s => s.DisplayName).Distinct();
            }
            catch (Exception e)
            {
                throw new ApplicationException("Failed to retrieve group members. See inner exception(s).", e);
            }
        }

        public (string DisplayName, string UserEmail) GetUserDetails(ClaimsPrincipal user)
        {
            var displayName = user.Claims.FirstOrDefault(c => c.Type.Equals("name", StringComparison.OrdinalIgnoreCase))?.Value;
            var userEmail = user.Claims.FirstOrDefault(c => c.Type.Equals("preferred_username", StringComparison.OrdinalIgnoreCase))?.Value;

            return (displayName, userEmail);
        }

    }
}

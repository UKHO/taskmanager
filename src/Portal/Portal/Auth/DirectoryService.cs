using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Portal.Configuration;
using User = Microsoft.Graph.User;

namespace Portal.Auth
{
    public class DirectoryService : IDirectoryService
    {
        private GraphServiceClient GraphClient { get; }

        public DirectoryService(IOptions<SecretsConfig> secretsConfig,
            IOptions<GeneralConfig> generalConfig, IOptions<UriConfig> uriConfig, bool isLocalDevelopment, IHttpProvider httpProvider)
        {
            var redirectUri = isLocalDevelopment ? $"{uriConfig.Value.LocalDevLandingPageHttpsUrl}signin-oidc" :
                $"{uriConfig.Value.LandingPageUrl}/signin-oidc";

            var authenticationProvider =
                new MsalAuthenticationProvider(generalConfig.Value.AzureAdClientId,
                    secretsConfig.Value.ClientAzureAdSecret,
                    generalConfig.Value.TenantId,
                    redirectUri);

            GraphClient = new GraphServiceClient(authenticationProvider, httpProvider);
        }

        public async Task<IEnumerable<string>> GetGroupMembers()
        {
            var groupName = "Hydrographic Data Team"; //TODO: URL encode?

            try
            {
                
                var groups = await GraphClient.Groups.Request().Expand("members").Filter($"displayName eq '{groupName}'").GetAsync();

                //TODO: Choose correct group
                var group = groups[0];

                if (group.Members == null || !group.Members.Any())
                {
                    //TODO: Log!
                    throw new ApplicationException($"Unable to get members of group {groupName}");
                }

                //TODO: Log count
                var members = group.Members.Select(o => ((User) o).DisplayName);

                return members;
            }
            catch (Exception e)
            {
                //TODO: Log!
                throw;
            }
        }
    }
}

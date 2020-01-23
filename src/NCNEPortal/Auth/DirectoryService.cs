using Microsoft.Extensions.Options;
using Microsoft.Graph;
using NCNEPortal.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NCNEPortal.Auth
{
    public class DirectoryService : IDirectoryService
    {
        private readonly IOptions<SecretsConfig> _secretsConfig;
        private GraphServiceClient GraphClient { get; }


        public DirectoryService(IOptions<SecretsConfig> secretsConfig,
            IOptions<GeneralConfig> generalConfig, IOptions<UriConfig> uriConfig, bool isLocalDevelopment, IHttpProvider httpProvider)
        {
            _secretsConfig = secretsConfig;
            var redirectUri = isLocalDevelopment ? $"{uriConfig.Value.LocalDevLandingPageHttpsUrl}signin-oidc" :
                $"{uriConfig.Value.LandingPageUrl}/signin-oidc";

            var authenticationProvider =
                new MsalAuthenticationProvider(generalConfig.Value.AzureAdClientId,
                    _secretsConfig.Value.ClientAzureAdSecret,
                    generalConfig.Value.TenantId,
                    redirectUri);

            GraphClient = new GraphServiceClient(authenticationProvider, httpProvider);
        }

        public async Task<IEnumerable<string>> GetGroupMembers()
        {
            var groups = new List<Guid> { _secretsConfig.Value.NcGuid, _secretsConfig.Value.NeGuid };

            var users = new List<DirectoryObject>();

            try
            {
                foreach (var groupId in groups)
                {
                    if (groupId != Guid.Empty)
                    {
                        var membersPage = await GraphClient.Groups[groupId.ToString()].Members.Request().GetAsync();
                        do
                        {
                            users.AddRange(membersPage?.CurrentPage);


                        } while (membersPage?.NextPageRequest != null &&
                                 (membersPage = await (membersPage?.NextPageRequest.GetAsync())).Count > 0);

                    }
                }

                if (users.Count == 0)
                {
                    throw new ApplicationException($"Unable to get members of group(s)");
                }

                return users.Select(o => ((User)o).DisplayName);
            }

            catch (Exception e)
            {
                //TODO: Log!
                throw new ApplicationException(e.Message);
            }
        }
    }
}
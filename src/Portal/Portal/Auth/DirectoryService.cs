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
            var groupId = _secretsConfig.Value.HDTGuid;

            try
            {
                var group = await GraphClient.Groups[groupId.ToString()].Request().Expand("members").GetAsync();

                if (group.Members == null || !group.Members.Any())
                {
                    //TODO: Log!
                    throw new ApplicationException($"Unable to get members of group.");
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

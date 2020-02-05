using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Portal.Configuration;
using User = Microsoft.Graph.User;

namespace Portal.Auth
{
    public class DirectoryService : IDirectoryService
    {
        private readonly IOptions<SecretsConfig> _secretsConfig;
        private readonly ILogger<DirectoryService> _logger;
        private GraphServiceClient GraphClient { get; }

        public DirectoryService(IOptions<SecretsConfig> secretsConfig,
            IOptions<GeneralConfig> generalConfig,
            IOptions<UriConfig> uriConfig,
            bool isLocalDevelopment,
            IHttpProvider httpProvider,
            ILogger<DirectoryService> logger)
        {
            _secretsConfig = secretsConfig;
            _logger = logger;
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
            var groups = new List<Guid>
            {
                _secretsConfig.Value.HDTGuid,
                _secretsConfig.Value.HDCGuid
            }.Where(groupId => groupId != Guid.Empty);

            if (!groups.Any()) throw new ApplicationException($"No GUIDs supplied.");

            var users = new List<DirectoryObject>();

            try
            {
                foreach (var groupId in groups)
                {
                    var membersPage = await GraphClient.Groups[groupId.ToString()].Members.Request().GetAsync();
                    do
                    {
                        users.AddRange(membersPage?.CurrentPage);

                    } while (membersPage?.NextPageRequest != null &&
                             (membersPage = await (membersPage?.NextPageRequest.GetAsync())).Count > 0);
                }

                if (users.Count == 0)
                {
                    throw new ApplicationException($"Unable to get members of group(s)");
                }

                return users.Select(o => ((User)o).DisplayName).OrderBy(s => s);
            }

            catch (Exception e)
            {
                throw new ApplicationException("Failed to retrieve group members", e);
            }
        }
    }
}

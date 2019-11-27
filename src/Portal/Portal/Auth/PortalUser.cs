using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Portal.Configuration;

namespace Portal.Auth
{
    public class PortalUser : IPortalUser
    {
        private GraphServiceClient GraphClient { get; }

        public PortalUser(IOptions<SecretsConfig> secretsConfig,
            IOptions<GeneralConfig> generalConfig, bool isLocalDevelopment)
        {
            var authenticationProvider =
                new MsalAuthenticationProvider(generalConfig.Value.AzureAdClientId,
                    secretsConfig.Value.ClientAzureAdSecret,
                    generalConfig.Value.TenantId,
                    isLocalDevelopment);

            GraphClient = new GraphServiceClient(authenticationProvider);
        }

        public async Task<string> GetFullNameForUser(ClaimsPrincipal user)
        {
            var graphUser = user.ToGraphUserAccount();
            var graphResult = await GraphClient.Users[graphUser.ObjectId].Request().GetAsync();
            return graphResult.DisplayName;
        }
    }
}

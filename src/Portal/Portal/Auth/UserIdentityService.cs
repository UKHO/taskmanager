using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Portal.Configuration;

namespace Portal.Auth
{
    public class UserIdentityService : IUserIdentityService
    {
        private GraphServiceClient GraphClient { get; }

        public UserIdentityService(IOptions<SecretsConfig> secretsConfig,
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

        public async Task<string> GetFullNameForUser(ClaimsPrincipal user)
        {
            var graphUser = user.ToGraphUserAccount();
            var graphResult = await GraphClient.Users[graphUser.ObjectId].Request().GetAsync();
            return graphResult.DisplayName;
        }

        public async Task<bool> ValidateUser(string username)
        {
            var graphUser = await GraphClient.Users.Request().Filter($"DisplayName eq '{username}'").GetAsync();

            return (graphUser?.Count > 0);
        }
    }
}

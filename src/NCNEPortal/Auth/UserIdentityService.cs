using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using NCNEPortal.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NCNEPortal.Auth
{
    public class UserIdentityService : NCNEPortal.Auth.IUserIdentityService
    {
        private GraphServiceClient GraphClient { get; }

        public UserIdentityService(IOptions<SecretsConfig> secretsConfig,
            IOptions<GeneralConfig> generalConfig, IOptions<UriConfig> uriConfig, bool isLocalDevelopment, IHttpProvider httpProvider)
        {
            //RS 14-Jan-2020 Commenting this code becuase the website works without this config
            /*var redirectUri = isLocalDevelopment ? $"{uriConfig.Value.LocalDevLandingPageHttpsUrl}signin-oidc" :
                $"{uriConfig.Value.LandingPageUrl}/signin-oidc"; */

            var authenticationProvider =
                new NCNEPortal.Auth.MsalAuthenticationProvider(generalConfig.Value.AzureAdClientId,
                    secretsConfig.Value.ClientAzureAdSecret,
                    generalConfig.Value.TenantId,
                    "");

            GraphClient = new GraphServiceClient(authenticationProvider, httpProvider);
        }

        public async Task<string> GetFullNameForUser(ClaimsPrincipal user)
        {
            var graphUser = user.ToGraphUserAccount();
            var graphResult = await GraphClient.Users[graphUser.ObjectId].Request().GetAsync();
            return graphResult.DisplayName;
        }
    }
}

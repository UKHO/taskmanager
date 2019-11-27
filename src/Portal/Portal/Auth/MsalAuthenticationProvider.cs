using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace Portal.Auth
{
    public class MsalAuthenticationProvider : IAuthenticationProvider
    {
        private IConfidentialClientApplication ClientApplication { get; }
        private List<string> Scopes { get; }

        public MsalAuthenticationProvider(string azureAdClientId,
                                          string azureAdSecret,
                                          string azureAdTenantId,
                                          bool isLocalDev)
        {
            // TODO look at keeping these all in config
            string redirectUri;
            if (isLocalDev)
            {
                redirectUri = "https://localhost:44308/signin-oidc";
            }
            else
            {
                // TODO no access to check if we have this setup in AAD as a reply URL under app reg.
                redirectUri = "http://taskmanager-dev-web-portal.azurewebsites.net/signin-oidc";
            }

            var authority = $"https://login.microsoftonline.com/{azureAdTenantId}/v2.0";

            ClientApplication = ConfidentialClientApplicationBuilder.Create(azureAdClientId)
                .WithAuthority(authority)
                .WithRedirectUri(redirectUri)
                .WithClientSecret(azureAdSecret)
                .Build();

            Scopes = new List<string>
            {
                "https://graph.microsoft.com/.default"
            };
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var token = await GetTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
        }

        public async Task<string> GetTokenAsync()
        {
            AuthenticationResult authResult = null;
            authResult = await ClientApplication.AcquireTokenForClient(Scopes)
                .ExecuteAsync();
            return authResult.AccessToken;
        }
    }
}

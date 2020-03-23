using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace Common.Helpers.Auth
{
    public class MsalAuthenticationProvider : IAuthenticationProvider
    {
        private IConfidentialClientApplication ClientApplication { get; }
        private List<string> Scopes { get; }

        public MsalAuthenticationProvider(string azureAdClientId,
                                          string azureAdSecret,
                                          string azureAdTenantId,
                                          string redirectUri)
        {
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

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages;
using Microsoft.Extensions.Options;
using Portal.Configuration;

namespace Portal.HttpClients
{
    public class EventServiceApiClient : IEventServiceApiClient
    {
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly HttpClient _httpClient;

        public EventServiceApiClient(HttpClient httpClient, IOptions<GeneralConfig> generalConfig, IOptions<UriConfig> uriConfig)
        {
            _httpClient = httpClient;
            _uriConfig = uriConfig;
        }

        public async Task PostEvent<T>(string eventName, T eventBody) where T : class, ICorrelate, new()
        {
            var data = "";
            var fullUri = _uriConfig.Value.BuildEventServicesUri(eventName);

            var content = new StringContent(eventBody.ToJSONSerializedString(), Encoding.UTF8, "application/json");

            using (var response = await _httpClient.PostAsync(fullUri.ToString(), content).ConfigureAwait(false))
            {
                data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }
        }
    }
}

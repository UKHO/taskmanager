using System;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Messages;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public async Task PostEvent<T>(string eventName, T eventBody) where T : ICorrelate
        {
            var data = "";
            var fullUri = _uriConfig.Value.BuildEventServicesUri(eventName);

            var eh = JsonConvert.SerializeObject(eventBody);

            using (var response = await _httpClient.PostAsync(fullUri.ToString(), null).ConfigureAwait(false))
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

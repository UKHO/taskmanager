using System;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Extensions.Options;
using WorkflowCoordinator.Config;

namespace WorkflowCoordinator.HttpClients
{
    public class PcpEventServiceApiClient : IPcpEventServiceApiClient
    {
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly HttpClient _httpClient;

        public PcpEventServiceApiClient(HttpClient httpClient, IOptions<UriConfig> uriConfig)
        {
            _httpClient = httpClient;
            _uriConfig = uriConfig;
        }

        public async Task PostEvent<T>(string eventName, T eventBody) where T : class, UKHO.Events.IUkhoEvent, new()
        {
            var data = "";
            var fullUri = _uriConfig.Value.BuildPcpEventServiceUri(eventName);

            var content = new StringContent(eventBody.ToJSONSerializedString(), System.Text.Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync(fullUri, content).ConfigureAwait(false);
            data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                               $"\n Message= '{data}'," +
                                               $"\n Url='{fullUri}'");
        }

    }
}
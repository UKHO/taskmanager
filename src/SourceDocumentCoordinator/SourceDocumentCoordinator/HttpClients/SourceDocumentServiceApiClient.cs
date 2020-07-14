using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SourceDocumentCoordinator.Config;

namespace SourceDocumentCoordinator.HttpClients
{
    public class SourceDocumentServiceApiClient : ISourceDocumentServiceApiClient
    {
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly HttpClient _httpClient;

        public SourceDocumentServiceApiClient(HttpClient httpClient, IOptions<UriConfig> uriConfig)
        {
            _httpClient = httpClient;
            _uriConfig = uriConfig;
        }

        public async Task<Guid> Post(int processId, int sdocId, string filepath)
        {
            var data = "";

            var content = new StringContent(filepath, System.Text.Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(_uriConfig.Value.SourceDocumentServiceBaseUrl, content).ConfigureAwait(false);

            data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                               $"\n Message= '{data}'," +
                                               $"\n Url='{_uriConfig.Value.SourceDocumentServiceBaseUrl}'");

            return Guid.Parse(data);
        }
    }
}

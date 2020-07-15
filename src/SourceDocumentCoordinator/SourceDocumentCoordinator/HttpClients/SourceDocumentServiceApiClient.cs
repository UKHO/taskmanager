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

        public async Task<Guid> Post(int processId, int sdocId, string filename)
        {
            var url = _uriConfig.Value.BuildSourceDocumentServicePostDocumentUri(processId, sdocId, filename);

            var response = await _httpClient.PostAsync(url, null).ConfigureAwait(false);

            var data = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                               $"\n Message= '{data}'," +
                                               $"\n Url='{_uriConfig.Value.SourceDocumentServiceBaseUrl}'");

            return System.Text.Json.JsonSerializer.Deserialize<Guid>(data);
        }
    }
}

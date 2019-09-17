using System;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Helpers;
using DataServices.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SourceDocumentCoordinator.Config;

namespace SourceDocumentCoordinator.HttpClients
{
    public class DataServiceApiClient : IDataServiceApiClient
    {
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly HttpClient _httpClient;

        public DataServiceApiClient(HttpClient httpClient, IOptions<GeneralConfig> generalConfig, IOptions<UriConfig> uriConfig)
        {
            _generalConfig = generalConfig;
            _httpClient = httpClient;
            _uriConfig = uriConfig;
        }

        public async Task<ReturnCode> GetDocumentForViewing(string callerCode, int sdocId, string writableFolderName, bool imageAsGeotiff)
        {
            var data = "";
            var baseUri = _uriConfig.Value.BuildDataServicesBaseUri();

            var fullUri = new Uri(baseUri,
                $"{_uriConfig.Value.DataServicesWebServiceGetDocumentForViewingUri}{callerCode}/{sdocId}/{writableFolderName}/{imageAsGeotiff}");

            using (var response = await _httpClient.GetAsync(fullUri.ToString()))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");

            }

            var returnCode = JsonConvert.DeserializeObject<ReturnCode>(data);

            return returnCode;
        }

        public async Task<bool> CheckDataServicesConnection()
        {
            var fullUri = new Uri(ConfigHelpers.IsLocalDevelopment ? $"{_uriConfig.Value.DataServicesLocalhostHealthcheckUrl}" :
                $"{_uriConfig.Value.DataServicesHealthcheckUrl}");

            using (var response = await _httpClient.GetAsync(fullUri.ToString()))
            {
                var data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }

            return true;
        }

        public async Task<QueuedDocumentObjects> GetDocumentRequestQueueStatus(string callerCode)
        {
            var data = "";
            var baseUri = _uriConfig.Value.BuildDataServicesBaseUri();

            var fullUri = new Uri(baseUri,
                $"{_uriConfig.Value.DataServicesWebServiceDocumentRequestQueueStatusUri}{callerCode}");

            using (var response = await _httpClient.GetAsync(fullUri.ToString()))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");

            }

            var returnCode = JsonConvert.DeserializeObject<QueuedDocumentObjects>(data);

            return returnCode;
        }

        public async Task<ReturnCode> DeleteDocumentRequestJobFromQueue(string callerCode, int sdocId, string writeableFolderName)
        {
            var data = "";
            var baseUri = _uriConfig.Value.BuildDataServicesBaseUri();

            var fullUri = new Uri(baseUri,
                $"{_uriConfig.Value.DataServicesWebServiceDeleteDocumentRequestJobFromQueueUri}{callerCode}/{sdocId}/{writeableFolderName}");

            using (var response = await _httpClient.GetAsync(fullUri.ToString()))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");

            }

            var returnCode = JsonConvert.DeserializeObject<ReturnCode>(data);

            return returnCode;
        }
    }
}

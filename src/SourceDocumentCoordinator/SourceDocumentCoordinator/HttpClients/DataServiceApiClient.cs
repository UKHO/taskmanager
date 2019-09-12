using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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

            var fullUri = new UriBuilder(baseUri)
            {
                Path =
                    $"{_uriConfig.Value.DataServicesWebServiceGetDocumentForViewingUri}{callerCode}/{sdocId}/{writableFolderName}/{imageAsGeotiff}"
            };

            using (var response = await _httpClient.GetAsync(fullUri.ToString()))
            {
                data = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");

            }

            var returnCode = JsonConvert.DeserializeObject<ReturnCode>(data);

            return returnCode;
        }
    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DataServices.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portal.Configuration;

namespace Portal.HttpClients
{
    public class DataServiceApiClient : IDataServiceApiClient
    {
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly HttpClient _httpClient;

        public DataServiceApiClient(HttpClient httpClient, IOptions<UriConfig> uriConfig)
        {
            _httpClient = httpClient;
            _uriConfig = uriConfig;
        }

        public async Task<DocumentAssessmentData> GetAssessmentData(int sdocId)
        {
            var fullUri = _uriConfig.Value.BuildDocumentAssessmentDataDataServicesUri(sdocId);

            using var response = await _httpClient.GetAsync(fullUri, HttpCompletionOption.ResponseHeadersRead);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                               $"\n Message= '{response.Content}'," +
                                               $"\n Url='{fullUri}'");

            var data = await response.Content.ReadAsStringAsync();
            var assessmentData = JsonConvert.DeserializeObject<DocumentAssessmentData>(data);
            
            return assessmentData;
        }

    }
}

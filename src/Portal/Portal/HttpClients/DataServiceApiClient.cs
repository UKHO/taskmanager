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

        public async
            Task<(DocumentAssessmentData assessmentData, HttpStatusCode httpStatusCode, string errorMessage, Uri fullUri
                )> GetAssessmentData(int sdocId)
        {
            var fullUri = _uriConfig.Value.BuildDocumentAssessmentDataDataServicesUri(sdocId);

            using var response = await _httpClient.GetAsync(fullUri, HttpCompletionOption.ResponseHeadersRead);
            var data = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var assessmentData = JsonConvert.DeserializeObject<DocumentAssessmentData>(data);

                return (assessmentData,
                    response.StatusCode,
                    string.Empty,
                    fullUri);
            }

            return (null,
                response.StatusCode,
                string.IsNullOrEmpty(data) ? "System failure fetching Source Document Data. Please try again later." : data,
                fullUri);
        }
    }
}

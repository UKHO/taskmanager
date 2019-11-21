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
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly HttpClient _httpClient;

        public DataServiceApiClient(HttpClient httpClient, IOptions<GeneralConfig> generalConfig, IOptions<UriConfig> uriConfig)
        {
            _generalConfig = generalConfig;
            _httpClient = httpClient;
            _uriConfig = uriConfig;
        }

        public async Task PutAssessmentCompleted(int sdocId, string comment)
        {
            var data = "";
            var fullUri = _uriConfig.Value.BuildDataServicesUri(_generalConfig.Value.CallerCode, sdocId, comment);

            using (var response = await _httpClient.PutAsync(fullUri.ToString(), null).ConfigureAwait(false))
            {
                data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }
        }

        public async Task<DocumentAssessmentData> GetAssessmentData(int sdocId)
        {
            var data = "";
            var fullUri = _uriConfig.Value.BuildDocumentAssessmentDataDataServicesUri(sdocId);

            using (var response = await _httpClient.GetAsync(fullUri))
            {
                data = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");

            }

            var assessmentData = JsonConvert.DeserializeObject<DocumentAssessmentData>(data);

            return assessmentData;
        }

    }
}

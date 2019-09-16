using DataServices.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Helpers;
using WorkflowCoordinator.Config;

namespace WorkflowCoordinator.HttpClients
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

        public async Task<IEnumerable<DocumentObject>> GetAssessments(string callerCode)
        {
            var data = "";
            var fullUri = _uriConfig.Value.BuildDataServicesUri(_generalConfig.Value.CallerCode, 0);

            using (var response = await _httpClient.GetAsync(fullUri))
            { 
                data = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");

            }

            var assessments = JsonConvert.DeserializeObject<IEnumerable<DocumentObject>>(data);

            return assessments;
        }

        public async Task<DocumentAssessmentData> GetAssessmentData(string callerCode, int sdocId)
        {
            var data = "";
            var fullUri = _uriConfig.Value.BuildDataServicesUri(_generalConfig.Value.CallerCode, sdocId);

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
    }
}

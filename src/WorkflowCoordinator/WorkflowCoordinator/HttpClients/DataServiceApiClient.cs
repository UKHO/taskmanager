using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DataServices.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WorkflowCoordinator.Config;

namespace WorkflowCoordinator.HttpClients
{
    public class DataServiceApiClient : IDataServiceApiClient
    {
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly HttpClient _httpClient;

        public DataServiceApiClient(HttpClient httpClient, IOptions<GeneralConfig> generalConfig)
        {
            _generalConfig = generalConfig;
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<DocumentObject>> GetAssessments(string callerCode)
        {
            // TODO look at what should move to config
            // TODO: wrap with try-catch to retrun a meaningfull response

            var data = "";
            var fullUri = new Uri($@"{_generalConfig.Value.DataServicesWebServiceUri}SourceDocument/Assessment/DocumentsForAssessment/{callerCode}");

            using (var response = await _httpClient.GetAsync(fullUri))
            {
                data = await response.Content.ReadAsStringAsync();
            }

            var assessments = JsonConvert.DeserializeObject<IEnumerable<DocumentObject>>(data);

            return assessments;
        }
    }
}

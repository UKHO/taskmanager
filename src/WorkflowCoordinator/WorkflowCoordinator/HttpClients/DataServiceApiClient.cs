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

            var response = await _httpClient.GetAsync($@"{_generalConfig.Value.DataAccessLocalhostBaseUri}SourceDocument/Assessment/DocumentsForAssessment/{callerCode}");

            var assessments = JsonConvert.DeserializeObject<IEnumerable<DocumentObject>>(await response.Content.ReadAsStringAsync());
            return assessments;
        }
    }
}

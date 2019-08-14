using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.Models;

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

        public async Task<IEnumerable<AssessmentModel>> GetAssessments(string callerCode)
        {
            // TODO look at what should move to config

            var response = await _httpClient.GetAsync($@"{_generalConfig.Value.DataAccessLocalhostBaseUri}SourceDocument/Assessment/DocumentsForAssessment/{callerCode}");

            var assessments = JsonConvert.DeserializeObject<IEnumerable<AssessmentModel>>(await response.Content.ReadAsStringAsync());
            return assessments;
        }
    }
}

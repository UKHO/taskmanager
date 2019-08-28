using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using WorkflowCoordinator.Config;
using WorkflowCoordinator.Models;

namespace WorkflowCoordinator.HttpClients
{
    public class WorkflowServiceApiClient : IWorkflowServiceApiClient
    {
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly HttpClient _httpClient;

        public WorkflowServiceApiClient(HttpClient httpClient, IOptions<GeneralConfig> generalConfig)
        {
            _generalConfig = generalConfig;
            _httpClient = httpClient;
        }

        public async Task<int> CreateWorkflowInstance()
        {
            //TODO: Get Workflows to get WorkflowID for "DB Assessment"; this is the main workflow id and not the instance id
            var dbAssessmentId = await GetDBAssessmentWorkflowId();

            if (dbAssessmentId ==0)
                throw new ApplicationException($"Failed to find {_generalConfig.Value.K2DBAssessmentWorkflowName} K2 workflow in {_generalConfig.Value.K2WebServiceBaseUri}");

            //TODO: Create Workflow Instance
            //TODO: Get SerialNumber
            return 0;
        }

        private async Task<int> GetDBAssessmentWorkflowId()
        {
            // TODO: Getting 401 Unauthorised even when using localhost; enabling Windows Auth in IIS; Diabling Anonymous in IIS
            //var localK2BaseUrl = new Uri("http://localhost:81/Api/");
            //Uri fullUri = new Uri(localK2BaseUrl, _generalConfig.Value.K2WebServiceGetWorkflowsUri);

            // TODO: Getting Certificate error when using https; 
            Uri fullUri = new Uri(_generalConfig.Value.K2WebServiceBaseUri, _generalConfig.Value.K2WebServiceGetWorkflowsUri);
           

            var response = await _httpClient.GetAsync(fullUri);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = await response.Content.ReadAsStringAsync();

                var workflows = JsonConvert.DeserializeObject<IEnumerable<K2WorkflowData>>(data);
                var dbAssesmentWorkflow = workflows.FirstOrDefault(w => w.Name.Equals(_generalConfig.Value.K2DBAssessmentWorkflowName, StringComparison.OrdinalIgnoreCase));
                return dbAssesmentWorkflow?.Id ?? 0;
            }

            return 0;
        }
    }
}

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

        public async Task<int> CreateWorkflowInstance(int dbAssessmentWorkflowId)
        {
            //if (dbAssessmentId ==0)
            //    throw new ApplicationException($"Failed to find {_generalConfig.Value.K2DBAssessmentWorkflowName} K2 workflow in {_generalConfig.Value.K2WebServiceBaseUri}");

            //TODO: Create Workflow Instance
            //TODO: Get SerialNumber
            return 0;
        }

        public async Task<int> GetDBAssessmentWorkflowId()
        {
            // TODO: Getting 401 Unauthorised even when using localhost; enabling Windows Auth in IIS; Diabling Anonymous in IIS
            //var localK2BaseUrl = new Uri("http://localhost:81/Api/");
            //Uri fullUri = new Uri(localK2BaseUrl, _generalConfig.Value.K2WebServiceGetWorkflowsUri);

            // TODO: Getting Certificate error when using https; 
            var fullUri = new Uri(_generalConfig.Value.K2WebServiceBaseUri, _generalConfig.Value.K2WebServiceGetWorkflowsUri);
            var data = "";

            using (var response = await _httpClient.GetAsync(fullUri))
            {
                data = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{_generalConfig.Value.K2WebServiceBaseUri}'");

            }

            var workflows = JsonConvert.DeserializeObject<K2Workflows>(data);
            var dbAssesmentWorkflow = workflows.Workflows.FirstOrDefault(w => w.Name.Equals(_generalConfig.Value.K2DBAssessmentWorkflowName, StringComparison.OrdinalIgnoreCase));
            return dbAssesmentWorkflow?.Id ?? 0;

        }
    }
}

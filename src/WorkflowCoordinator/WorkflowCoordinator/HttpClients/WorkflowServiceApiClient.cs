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
            var fullUri = new Uri(_generalConfig.Value.K2WebServiceBaseUri, _generalConfig.Value.K2WebServiceStartWorkflowInstanceUri + $"/{dbAssessmentWorkflowId}");
            var data = "";

            using (var response = await _httpClient.PostAsync(fullUri, null))
            {
                data = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{_generalConfig.Value.K2WebServiceBaseUri}'");

            }

            if (!int.TryParse(data, out var workflowInstanceId))
            {
                throw new ApplicationException($"Failed to get WorkflowInstanceId" +
                                               $"\nData= '{data}'," +
                                               $"\n Url='{_generalConfig.Value.K2WebServiceBaseUri}'");

            }

            return workflowInstanceId;
        }

        public async Task<int> GetDBAssessmentWorkflowId()
        {
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

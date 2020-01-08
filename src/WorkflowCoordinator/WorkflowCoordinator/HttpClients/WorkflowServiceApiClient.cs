using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.Models;

namespace WorkflowCoordinator.HttpClients
{
    public class WorkflowServiceApiClient : IWorkflowServiceApiClient
    {
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly HttpClient _httpClient;

        public WorkflowServiceApiClient(HttpClient httpClient, IOptions<GeneralConfig> generalConfig, IOptions<UriConfig> uriConfig)
        {
            _generalConfig = generalConfig;
            _uriConfig = uriConfig;
            _httpClient = httpClient;
        }

        public async Task<int> CreateWorkflowInstance(int dbAssessmentWorkflowId)
        {
            var fullUri = new Uri(_uriConfig.Value.K2WebServiceBaseUri, _uriConfig.Value.K2WebServiceStartWorkflowInstanceUri + $"/{dbAssessmentWorkflowId}");
            var data = "";

            using (var response = await _httpClient.PostAsync(fullUri, null))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }

            if (!int.TryParse(data, out var workflowInstanceId))
            {
                throw new ApplicationException($"Failed to get WorkflowInstanceId" +
                                               $"\nData= '{data}'," +
                                               $"\n Url='{fullUri}'");

            }

            return workflowInstanceId;
        }

        public async Task<int> GetDBAssessmentWorkflowId()
        {
            var fullUri = new Uri(_uriConfig.Value.K2WebServiceBaseUri, _uriConfig.Value.K2WebServiceGetWorkflowsUri);
            string data;

            using (var response = await _httpClient.GetAsync(fullUri))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");

            }

            var workflows = JsonConvert.DeserializeObject<K2Workflows>(data);
            var dbAssesmentWorkflow = workflows.Workflows.First(w => w.Name.Equals(_generalConfig.Value.K2DBAssessmentWorkflowName, StringComparison.OrdinalIgnoreCase));
            return dbAssesmentWorkflow.Id;

        }

        public async Task<string> GetWorkflowInstanceSerialNumber(int workflowInstanceId)
        {
            var fullUri = new Uri(_uriConfig.Value.K2WebServiceBaseUri, _uriConfig.Value.K2WebServiceGetTasksUri);
            string data;

            using (var response = await _httpClient.GetAsync(fullUri))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");

            }

            var tasks = JsonConvert.DeserializeObject<K2Tasks>(data);
            var task = tasks.Tasks.First(w => w.WorkflowInstanceID == workflowInstanceId);

            return task.SerialNumber;
        }

        public async Task TerminateWorkflowInstance(string serialNumber)
        {
            var fullUri = new Uri(_uriConfig.Value.K2WebServiceBaseUri,
                $"{_uriConfig.Value.K2WebServiceGetTasksUri}{serialNumber}/{_uriConfig.Value.K2WebServiceTerminateWorkflowInstanceUri}");
            var data = "";

            using (var response = await _httpClient.PostAsync(fullUri, null))
            {
                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }

        }

        public async Task ProgressWorkflowInstance(string serialNo, string action)
        {
            var fullUri = new Uri(_uriConfig.Value.K2WebServiceBaseUri, _uriConfig.Value.K2WebServiceGetTasksUri + $"{serialNo}" + "/actions/" + $"/{action}");

            var data = "";

            using (var response = await _httpClient.PostAsync(fullUri, null))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }

            if (!int.TryParse(data, out var workflowInstanceId))
            {
                throw new ApplicationException($"Failed to get WorkflowInstanceId" +
                                               $"\nData= '{data}'," +
                                               $"\n Url='{fullUri}'");

            }

            //return workflowInstanceId;
        }
    }
}

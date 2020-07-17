using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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

        public async Task<bool> CheckK2Connection()
        {
            var fullUri = new Uri(_uriConfig.Value.K2WebServiceBaseUri, _uriConfig.Value.K2WebServiceGetWorkflowsUri);

            using (var response = await _httpClient.GetAsync(fullUri))
            {
                var data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return false;

            }

            return true;

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
            var dbAssesmentWorkflow = workflows.Workflows.First(w => w.Name == _generalConfig.Value.K2DBAssessmentWorkflowName);
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
            
            if (tasks == null || tasks.ItemCount == 0 || tasks.Tasks == null || !tasks.Tasks.Any())
            {
                return null;
            }

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

        public async Task<bool> ProgressWorkflowInstance(string k2SerialNumber)
        {
            var fullUri = new Uri(_uriConfig.Value.K2WebServiceBaseUri, _uriConfig.Value.K2WebServiceGetTasksUri + $"{k2SerialNumber}/actions/Approve");

            using (var response = await _httpClient.PostAsync(fullUri, null))
            {
                var data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }

            return true;
        }

        public async Task<K2TaskData> GetWorkflowInstanceData(int workflowInstanceId)
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

            if (tasks == null || tasks.ItemCount == 0 || tasks.Tasks == null || !tasks.Tasks.Any())
            {
                return null;
            }

            return tasks.Tasks.FirstOrDefault(w => w.WorkflowInstanceID == workflowInstanceId);
        }
        
        public async Task<bool> RejectWorkflowInstance(string serialNumber)
        {

            var fullUri = new Uri(_uriConfig.Value.K2WebServiceBaseUri, _uriConfig.Value.K2WebServiceGetTasksUri + $"{serialNumber}/actions/Reject");

            using (var response = await _httpClient.PostAsync(fullUri, null))
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

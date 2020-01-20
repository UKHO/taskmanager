using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portal.Configuration;
using Portal.Models;
using Portal.Pages.DbAssessment;
using Serilog.Context;

namespace Portal.HttpClients
{
    public class WorkflowServiceApiClient : IWorkflowServiceApiClient
    {
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly ILogger<WorkflowServiceApiClient> _logger;
        private readonly HttpClient _httpClient;

        public WorkflowServiceApiClient(
                                        HttpClient httpClient,
                                        IOptions<UriConfig> uriConfig,
                                        ILogger<WorkflowServiceApiClient> logger)
        {
            _uriConfig = uriConfig;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<bool> CheckK2Connection()
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

            return true;

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

        public async Task<string> GetTaskCurrentStage(string serialNumber)
        {
            var fullUri = new Uri(_uriConfig.Value.K2WebServiceBaseUri,
                $"{_uriConfig.Value.K2WebServiceGetTasksUri}{serialNumber}");
            var data = "";

            using (var response = await _httpClient.GetAsync(fullUri))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }

            var tasks = JsonConvert.DeserializeObject<K2Task>(data);

            return tasks.ActivityName;
        }

        public async Task<bool> ProgressWorkflowInstance(int processId,string serialNumber, string currentTaskStage, string progressToTaskStage)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("SerialNumber", serialNumber);
            LogContext.PushProperty("ActivityName", currentTaskStage);
            LogContext.PushProperty("ProgressToTaskStage", progressToTaskStage);
            LogContext.PushProperty("PortalResource", nameof(ProgressWorkflowInstance));

            _logger.LogInformation("Entering {PortalResource} with: ProcessId: {ProcessId}; "
                                            + "SerialNumber: {SerialNumber}; "
                                            + "CurrentTaskStage: {ActivityName}; "
                                            + "ProgressToTaskStage: {ProgressToTaskStage}");

            var taskStageInK2 = await GetTaskCurrentStage(serialNumber);

            LogContext.PushProperty("TaskStageInK2", taskStageInK2);

            if (taskStageInK2.Equals(progressToTaskStage))
            {
                _logger.LogInformation("{PortalResource} with: ProcessId: {ProcessId}; "
                                       + "SerialNumber: {SerialNumber}; "
                                       + "is already at the TaskStageInK2: {TaskStageInK2}");

                return true;
            }

            if (!taskStageInK2.Equals(currentTaskStage))
            {
                _logger.LogInformation("{PortalResource} with: ProcessId: {ProcessId}; "
                                       + "SerialNumber: {SerialNumber}; "
                                       + "is at an unexpected stage in K2 of the TaskStageInK2: {TaskStageInK2}");

                throw new ApplicationException(
                    $"K2 task with Process Id {processId} and Serial Number {serialNumber} is not at the expected stage {currentTaskStage} but instead its at {taskStageInK2}");
            }

            var fullUri = new Uri(_uriConfig.Value.K2WebServiceBaseUri, _uriConfig.Value.K2WebServiceGetTasksUri + $"{serialNumber}/actions/Approve");

            using (var response = await _httpClient.PostAsync(fullUri, null))
            {
                var data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }

            _logger.LogInformation("{PortalResource} with: ProcessId: {ProcessId}; "
                                   + "SerialNumber: {SerialNumber}; "
                                   + "successfully progressed the task to ProgressToTaskStage: {ProgressToTaskStage}");


            return true;
        }

        public async Task<K2Task> GetWorkflowInstanceData(int workflowInstanceId)
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
            return tasks.Tasks.First(w => w.WorkflowInstanceID == workflowInstanceId);
        }
    }
}

using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using Portal.Configuration;
using Portal.Models;

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Portal.HttpClients
{
    public class WorkflowServiceApiClient : IWorkflowServiceApiClient
    {
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly HttpClient _httpClient;

        public WorkflowServiceApiClient(HttpClient httpClient, IOptions<UriConfig> uriConfig)
        {
            _uriConfig = uriConfig;
            _httpClient = httpClient;
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
    }
}

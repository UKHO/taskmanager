using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Portal.Configuration;

namespace Portal.HttpClients
{
    public class WorkflowServiceApiClient : IWorkflowServiceApiClient
    {
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly HttpClient _httpClient;

        public WorkflowServiceApiClient(HttpClient httpClient, IOptions<GeneralConfig> generalConfig, IOptions<UriConfig> uriConfig)
        {
            _uriConfig = uriConfig;
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
    }
}

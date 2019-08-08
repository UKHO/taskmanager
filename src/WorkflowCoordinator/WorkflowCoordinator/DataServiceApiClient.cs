using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace WorkflowCoordinator
{
    public class DataServiceApiClient : IDataServiceApiClient
    {
        public IOptionsSnapshot<UrlsConfig> _urlsConfig;
        private readonly HttpClient _httpClient;

        public DataServiceApiClient(HttpClient httpClient, IOptionsSnapshot<UrlsConfig> urlsConfig)
        {
            _urlsConfig = urlsConfig;
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<Assessment>> GetAssessments(string callerCode)
        {
            var response = await _httpClient.GetAsync($@"{_urlsConfig.Value.BaseUrl}**HIDDEN**");

            var assessments = JsonConvert.DeserializeObject<IEnumerable<Assessment>>(await response.Content.ReadAsStringAsync());
            return assessments;
        }

        public class Assessment
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string SourceName { get; set; }
        }

    }

    public interface IDataServiceApiClient
    {
        Task<IEnumerable<DataServiceApiClient.Assessment>> GetAssessments(string callerCode);
    }
}

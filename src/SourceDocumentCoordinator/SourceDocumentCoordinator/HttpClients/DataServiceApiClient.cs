using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Common.Helpers;
using DataServices.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SourceDocumentCoordinator.Config;

namespace SourceDocumentCoordinator.HttpClients
{
    public class DataServiceApiClient : IDataServiceApiClient
    {
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly HttpClient _httpClient;

        public DataServiceApiClient(HttpClient httpClient, IOptions<GeneralConfig> generalConfig, IOptions<UriConfig> uriConfig)
        {
            _generalConfig = generalConfig;
            _httpClient = httpClient;
            _uriConfig = uriConfig;
        }

        public async Task<DocumentAssessmentData> GetAssessmentData(int sdocId)
        {
            var data = "";
            var fullUri = _uriConfig.Value.BuildDataServicesUri(sdocId);

            using (var response = await _httpClient.GetAsync(fullUri))
            {
                data = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");

            }

            var assessmentData = JsonConvert.DeserializeObject<DocumentAssessmentData>(data);

            return assessmentData;
        }

        public async Task<LinkedDocuments> GetBackwardDocumentLinks(int sdocId)
        {
            var data = "";
            var baseUri = _uriConfig.Value.BuildDataServicesBaseUri();
            var fullUri = new Uri(baseUri,
                $"{_uriConfig.Value.DataServicesWebServiceBackwardLinksUri}{sdocId}");

            using (var response = await _httpClient.GetAsync(fullUri.ToString()))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }

            var linkedDocuments = JsonConvert.DeserializeObject<LinkedDocuments>(data);

            return linkedDocuments;
        }

        public async Task<LinkedDocuments> GetForwardDocumentLinks(int sdocId)
        {
            var data = "";
            var baseUri = _uriConfig.Value.BuildDataServicesBaseUri();
            var fullUri = new Uri(baseUri,
                $"{_uriConfig.Value.DataServicesWebServiceForwardLinksUri}{sdocId}");

            using (var response = await _httpClient.GetAsync(fullUri.ToString()))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }

            var linkedDocuments = JsonConvert.DeserializeObject<LinkedDocuments>(data);

            return linkedDocuments;
        }

        public async Task<DocumentObjects> GetSepDocumentLinks(int sdocId)
        {
            var data = "";
            var baseUri = _uriConfig.Value.BuildDataServicesBaseUri();
            var fullUri = new Uri(baseUri,
                $"{_uriConfig.Value.DataServicesWebServiceSepLinksUri}{sdocId}");

            using (var response = await _httpClient.GetAsync(fullUri.ToString()))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }

            var docObjects = JsonConvert.DeserializeObject<DocumentObjects>(data);

            return docObjects;
        }

        public async Task<DocumentObjects> GetDocumentsFromList(int[] linkedDocsId)
        {
            var data = "";
            var baseUri = _uriConfig.Value.BuildDataServicesBaseUri();
            var queryString =
                CreateUrlQueryStringForArray(_uriConfig.Value.DataServicesWebServiceDocumentsFromListUriSdocIdQuery,
                    linkedDocsId);
            var fullUri = new Uri(baseUri,
                $"{_uriConfig.Value.DataServicesWebServiceDocumentsFromListUri}?{queryString}");

            using (var response = await _httpClient.GetAsync(fullUri.ToString()))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }

            var docObjects = JsonConvert.DeserializeObject<DocumentObjects>(data);

            return docObjects;
        }

        private string CreateUrlQueryStringForArray(string queryTerm, IEnumerable<int> values)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var value in values)
            {
                sb.Append(queryTerm);
                sb.Append("=");
                sb.Append(value.ToString());
                sb.Append("&");
            }

            sb.Remove(sb.Length-1, 1);

            return sb.ToString();
        }

        public async Task<ReturnCode> GetDocumentForViewing(string callerCode, int sdocId, string writableFolderName, bool imageAsGeotiff)
        {
            var data = "";
            var baseUri = _uriConfig.Value.BuildDataServicesBaseUri();

            var fullUri = new Uri(baseUri,
                $"{_uriConfig.Value.DataServicesWebServiceGetDocumentForViewingUri}{callerCode}/{sdocId}/{imageAsGeotiff}?writeableFolderName={Uri.EscapeDataString(writableFolderName)}");

            using (var response = await _httpClient.GetAsync(fullUri.ToString()))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");

            }

            var returnCode = JsonConvert.DeserializeObject<ReturnCode>(data);

            return returnCode;
        }

        public async Task<bool> CheckDataServicesConnection()
        {
            var fullUri = new Uri(ConfigHelpers.IsLocalDevelopment ? $"{_uriConfig.Value.DataServicesLocalhostHealthcheckUrl}" :
                $"{_uriConfig.Value.DataServicesHealthcheckUrl}");

            using (var response = await _httpClient.GetAsync(fullUri.ToString()))
            {
                var data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }

            return true;
        }

        public async Task<QueuedDocumentObjects> GetDocumentRequestQueueStatus(string callerCode)
        {
            var data = "";
            var baseUri = _uriConfig.Value.BuildDataServicesBaseUri();

            var fullUri = new Uri(baseUri,
                $"{_uriConfig.Value.DataServicesWebServiceDocumentRequestQueueStatusUri}{callerCode}");

            using (var response = await _httpClient.GetAsync(fullUri.ToString()))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");

            }

            var returnCode = JsonConvert.DeserializeObject<QueuedDocumentObjects>(data);

            return returnCode;
        }

        public async Task<ReturnCode> DeleteDocumentRequestJobFromQueue(string callerCode, int sdocId, string writeableFolderName)
        {
            var data = "";
            var baseUri = _uriConfig.Value.BuildDataServicesBaseUri();

            var fullUri = new Uri(baseUri,
                $"{_uriConfig.Value.DataServicesWebServiceDeleteDocumentRequestJobFromQueueUri}{callerCode}/{sdocId}?writeableFolderName={Uri.EscapeDataString(writeableFolderName)}");

            using (var response = await _httpClient.DeleteAsync(fullUri))
            {
                data = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");

            }

            var returnCode = JsonConvert.DeserializeObject<ReturnCode>(data);

            return returnCode;
        }
    }
}

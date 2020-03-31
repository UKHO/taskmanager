﻿using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DataServices.Models;
using Microsoft.Extensions.Options;
using Portal.Configuration;

namespace Portal.HttpClients
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

        public async Task MarkAssessmentAsCompleted(int sdocId, string comment)
        {
            var data = "";
            var fullUri = _uriConfig.Value.BuildDataServicesMarkAssessmentCompletedUri(_generalConfig.Value.CallerCode, sdocId, comment);

            using (var response = await _httpClient.PutAsync(fullUri.ToString(), null).ConfigureAwait(false))
            {
                data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                                   $"\n Message= '{data}'," +
                                                   $"\n Url='{fullUri}'");
            }
        }

        public async Task<DocumentAssessmentData> GetAssessmentData(int sdocId)
        {
            var fullUri = _uriConfig.Value.BuildDocumentAssessmentDataDataServicesUri(sdocId);

            using var response = await _httpClient.GetAsync(fullUri, HttpCompletionOption.ResponseHeadersRead);

            var data = await response.Content.ReadAsStreamAsync();
            var assessmentData = await JsonSerializer.DeserializeAsync<DocumentAssessmentData>(data);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new ApplicationException($"StatusCode='{response.StatusCode}'," +
                                               $"\n Message= '{response.Content}'," +
                                               $"\n Url='{fullUri}'");
            return assessmentData;
        }

    }
}

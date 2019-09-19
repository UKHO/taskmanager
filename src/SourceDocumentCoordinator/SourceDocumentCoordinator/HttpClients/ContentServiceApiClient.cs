using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.Models;

namespace SourceDocumentCoordinator.HttpClients
{
    public class ContentServiceApiClient : IContentServiceApiClient
    {
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly IOptions<SecretsConfig> _secretsConfig;
        private readonly HttpClient _httpClient;

        public ContentServiceApiClient(HttpClient httpClient, IOptions<UriConfig> uriConfig, IOptions<SecretsConfig> secretsConfig)
        {
            _httpClient = httpClient;
            _uriConfig = uriConfig;
            _secretsConfig = secretsConfig;
        }

        public async Task<Guid> Post(byte[] fileBytes, string filename)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var byteArrayContent = new ByteArrayContent(fileBytes);
            byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse(DetermineFileType(filename));

            var response = await _httpClient.PostAsync(_uriConfig.Value.ContentServiceBaseUrl, new MultipartFormDataContent
            {
                {new StringContent(JsonConvert.SerializeObject(ConstructContentServicePostRequest()), Encoding.UTF8, "application/json"),  "cspostrequest"},
                {byteArrayContent, "file", filename}

            });

            var deserializedResponse = JsonConvert.DeserializeObject<ContentServiceResponse>(await response.Content.ReadAsStringAsync());

            return GuidHelpers.ExtractGuidFromString(deserializedResponse.Properties.MetaData.Description);
        }

        private static string DetermineFileType(string filename)
        {
            var ext = Path.GetExtension(filename).ToUpper();

            switch (ext)
            {
                case ".PDF":
                    return "application/pdf";
                case ".ZIP":
                    return "application/zip";
                default:
                    throw new NotImplementedException();
            }
        }

        private static ContentServicePostRequest ConstructContentServicePostRequest()
        {
            var metadata = new ContentServiceMetadata
            {
                ProductType = "SDRASourceDocument", // TODO: Move to Azure app config
                UpdatedDate = DateTime.Today
            };

            var postRequest = new ContentServicePostRequest
            {
                Metadata = metadata,
                Tag = Guid.NewGuid().ToString()
            };
            return postRequest;
        }
    }
}

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SourceDocumentCoordinator.Config;

namespace SourceDocumentCoordinator.HttpClients
{
    public class ContentServiceApiClient : IContentServiceApiClient
    {
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly HttpClient _httpClient;

        public ContentServiceApiClient(HttpClient httpClient, IOptions<UriConfig> uriConfig)
        {
            _httpClient = httpClient;
            _uriConfig = uriConfig;
        }

        public async Task<Guid> Post()
        {
            var bytes = System.IO.File.ReadAllBytes("c:\\temp\\2.pdf");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("cspostrequest", "{\"Tag\":\"cid-48234096\",\"MetaData\":{\"ProductType\":\"SNCChartComponent\",\"UpdatedDate\":\"20150728\",\"HPDChartVersion\":\"12\"}}");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var byteArrayContent = new ByteArrayContent(bytes);
            byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");

            var response = await _httpClient.PostAsync(_uriConfig.Value.ContentServiceBaseUrl, new MultipartFormDataContent
            {
                {byteArrayContent, "file", "2.pdf"}
            });

            return Guid.NewGuid();
        }
    }
}

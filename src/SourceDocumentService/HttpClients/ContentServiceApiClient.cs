using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Common.Helpers;
using SourceDocumentService.Models;

namespace SourceDocumentService.HttpClients
{
    public class ContentServiceApiClient : IContentServiceApiClient
    {
        private readonly HttpClient _httpClient;

        public ContentServiceApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Guid> Post(byte[] fileBytes, string filename)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var byteArrayContent = new ByteArrayContent(fileBytes);
            byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse(DetermineFileType(filename));

            var response = await _httpClient.PostAsync(ConfigurationManager.AppSettings["ContentServiceBaseUrl"],
                new MultipartFormDataContent
                {
                    {
                        new StringContent(
                            System.Text.Json.JsonSerializer.Serialize(ConstructContentServicePostRequest()),
                            Encoding.UTF8, "application/json"),
                        "cspostrequest"
                    },
                    {byteArrayContent, "file", filename}

                });

            var deserializedResponse =
                System.Text.Json.JsonSerializer.Deserialize<ContentServiceResponse>(
                    await response.Content.ReadAsStringAsync());

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
                case ".TXT":
                    return "text/plain";
                case ".TIF":
                case ".TIFF":
                    return "image/tiff";
                case ".XLS":
                    return "application/vnd.ms-excel";
                case ".7Z":
                    return "application/x-7z-compressed";
                default:
                    throw new NotImplementedException();
            }
        }

        private static ContentServicePostRequest ConstructContentServicePostRequest()
        {
            var metadata = new ContentServiceMetadata
            {
                ProductType = "SDRASourceDocument",
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
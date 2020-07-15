using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Helpers;
using NUnit.Framework;
using SourceDocumentService.HttpClients;

namespace SourceDocumentService.IntegrationTests
{
    [TestFixture]
    public class SourceDocumentServiceIntegrationTests
    {
        private IContentServiceApiClient _contentServiceApiClient;

        [Test]
        public async Task Test_ContentService_Connectivity()
        {
            var testHandler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential(_secretsConfigWrapper.Value.ContentServiceUsername,
                    _secretsConfigWrapper.Value.ContentServicePassword,
                    _secretsConfigWrapper.Value.ContentServiceDomain)
            };

            var httpClient = new HttpClient(testHandler);
            _contentServiceApiClient = new ContentServiceApiClient(httpClient, _uriConfigWrapper);

            var filename = Path.Combine(Environment.CurrentDirectory, "TestData") + "\\ContentServiceIntegration.txt";

            var returnedGuid = await _contentServiceApiClient.Post(File.ReadAllBytes(filename), "ContentServiceIntegration.txt");

            Assert.IsNotNull(returnedGuid);
            Assert.AreNotEqual(Guid.NewGuid(), returnedGuid);
        }
    }
}

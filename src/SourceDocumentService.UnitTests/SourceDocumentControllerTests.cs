using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using SourceDocumentService.Configuration;
using SourceDocumentService.Controllers;
using SourceDocumentService.HttpClients;

namespace SourceDocumentService.UnitTests
{
    [TestFixture]
    public class SourceDocumentControllerTests
    {
        private IFileSystem _fakeFileSystem;
        private SourceDocumentController _sourceDocumentController;
        private ILogger<SourceDocumentController> _fakeLogger;
        private IContentServiceApiClient _fakeContentServiceApiClient;
        private IConfigurationManager _fakeConfigurationManager;
        private int _processId;
        private int _sDocId;

        [SetUp]
        public void Setup()
        {
            _processId = 123;
            _sDocId = 12345;

            _fakeContentServiceApiClient = A.Fake<IContentServiceApiClient>();
            _fakeLogger = A.Fake<ILogger<SourceDocumentController>>();
            _fakeFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                {@"myfile.txt", new MockFileData("Testing is meh.")},
                {@"jQuery.js", new MockFileData("some js")},
                { "c:\\test\\TestImage.tif", new MockFileData(new byte[] {0x12, 0x34, 0x56, 0xd2})}
            });
            _fakeConfigurationManager = A.Fake<IConfigurationManager>();

            A.CallTo(() => _fakeConfigurationManager.GetAppSetting("FilestorePath")).Returns("c:\\test\\");

            _sourceDocumentController =
                new SourceDocumentController(_fakeLogger, _fakeFileSystem, _fakeContentServiceApiClient, _fakeConfigurationManager);
        }

        [Test]
        public void Test_when_document_does_not_exist_then_FileNotFoundException_thrown()
        {
            Assert.ThrowsAsync<FileNotFoundException>(() => _sourceDocumentController.PostSourceDocumentToContentService(_processId,
                _sDocId, @"notexist.txt"));
        }

        [Test]
        public async Task Test_When_document_exists_then_ContentService_call_made()
        {
            A.CallTo(() => _fakeContentServiceApiClient.Post(A<byte[]>.Ignored, A<string>.Ignored)).Returns(Guid.NewGuid());

            await _sourceDocumentController.PostSourceDocumentToContentService(_processId, _sDocId, @"TestImage.tif");
            A.CallTo(() => _fakeContentServiceApiClient.Post(A<byte[]>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();

        }
    }
}

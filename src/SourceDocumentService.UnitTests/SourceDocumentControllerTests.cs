using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
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
                {@"TestImage.tif", new MockFileData(new byte[] {0x12, 0x34, 0x56, 0xd2})}
            });

            _sourceDocumentController =
                new SourceDocumentController(_fakeLogger, _fakeFileSystem, _fakeContentServiceApiClient);
        }

        [Test]
        public async Task Test_When_Document_does_not_exist_Then_FileNotFoundException_thrown_and_Document_Status_and_FileLocation_is_not_Updated()
        {
            Assert.ThrowsAsync<FileNotFoundException>(() => _sourceDocumentController.PostSourceDocumentToContentService(_processId,
                _sDocId, @"notexist.txt"));
        }
    }
}

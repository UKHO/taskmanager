using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using Common.Messages.Enums;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using NUnit.Framework;
using SourceDocumentCoordinator.Handlers;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace SourceDocumentCoordinator.UnitTests
{
    public class PersistDocumentInStoreCommandHandlerTests
    {
        private ISourceDocumentServiceApiClient _fakeSourceDocumentServiceApiClient;
        private IFileSystem _fakeFileSystem;
        private TestableMessageHandlerContext _handlerContext;
        private PersistDocumentInStoreCommandHandler _handler;
        private ILogger<PersistDocumentInStoreCommandHandler> _fakeLogger;
        private WorkflowDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeSourceDocumentServiceApiClient = A.Fake<ISourceDocumentServiceApiClient>();
            _fakeFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\myfile.txt", new MockFileData("Testing is meh.") },
                { @"c:\demo\jQuery.js", new MockFileData("some js") },
                { @"c:\test\TestImage.tif", new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) }
            });
            _fakeLogger = A.Fake<ILogger<PersistDocumentInStoreCommandHandler>>();

            _handlerContext = new TestableMessageHandlerContext();
            _handler = new PersistDocumentInStoreCommandHandler(_fakeSourceDocumentServiceApiClient, _dbContext, _fakeLogger, _fakeFileSystem);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_When_Primary_Document_Successfully_posted_to_SourceDocumentService_Then_Document_Status_and_FileLocation_is_Updated()
        {
            //Given
            var message = new PersistDocumentInStoreCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 5678,
                SourceDocumentId = 1999999,
                SourceType = SourceType.Primary,
                Filepath = @"c:\test\TestImage.tif"
            };

            A.CallTo(() =>
                    _fakeSourceDocumentServiceApiClient.Post(message.ProcessId, message.SourceDocumentId, message.Filepath))
                .Returns(Guid.NewGuid());

            // when
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Assert
            // TODO: Assert on DB instead of factory
        }

        [Test]
        public async Task Test_When_Linked_Document_Successfully_posted_to_SourceDocumentService_Then_Document_Status_and_FileLocation_is_Updated()
        {
            var linkedDocUniqueId = Guid.NewGuid();

            _dbContext.LinkedDocument.Add(new LinkedDocument
            {
                ProcessId = 5678,
                LinkType = "Forward",
                Created = DateTime.Now.Date,
                UniqueId = linkedDocUniqueId,
                LinkedSdocId = 1999999,
                Status = "Started"
            });

            await _dbContext.SaveChangesAsync();

            //Given
            var message = new PersistDocumentInStoreCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 5678,
                SourceDocumentId = 1999999,
                SourceType = SourceType.Linked,
                Filepath = @"c:\test\TestImage.tif",
                UniqueId = linkedDocUniqueId
            };

            A.CallTo(() =>
                    _fakeSourceDocumentServiceApiClient.Post(message.ProcessId, message.SourceDocumentId, message.Filepath))
                .Returns(Guid.NewGuid());

            // when
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Assert
            // TODO: Assert on DB instead of factory

        }
        
        [Test]
        public async Task Test_When_Database_Document_Successfully_posted_to_SourceDocumentService_Then_Document_Status_and_FileLocation_is_Updated()
        {
            //Given
            var message = new PersistDocumentInStoreCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 5678,
                SourceDocumentId = 1999999,
                SourceType = SourceType.Database,
                Filepath = @"c:\test\TestImage.tif"
            };

            A.CallTo(() =>
                    _fakeSourceDocumentServiceApiClient.Post(message.ProcessId, message.SourceDocumentId, message.Filepath))
                .Returns(Guid.NewGuid());

            // when
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Assert
            // TODO: Assert on DB instead of factory

        }
    }
}

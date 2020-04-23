using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using Common.Factories.Interfaces;
using Common.Messages.Enums;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NServiceBus.Testing;
using NUnit.Framework;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.Handlers;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;
using WorkflowDatabase.EF;

namespace SourceDocumentCoordinator.UnitTests
{
    public class PersistDocumentInStoreCommandHandlerTests
    {
        private IContentServiceApiClient _fakeContentServiceApiClient;
        private IDocumentStatusFactory _fakeDocumentStatusFactory;
        private IDocumentFileLocationFactory _fakeDocumentFileLocationFactory;
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

            _fakeContentServiceApiClient = A.Fake<IContentServiceApiClient>();
            _fakeDocumentStatusFactory = A.Fake<IDocumentStatusFactory>();
            _fakeDocumentFileLocationFactory = A.Fake<IDocumentFileLocationFactory>();
            _fakeFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\myfile.txt", new MockFileData("Testing is meh.") },
                { @"c:\demo\jQuery.js", new MockFileData("some js") },
                { @"c:\test\TestImage.tif", new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) }
            });
            _fakeLogger = A.Fake<ILogger<PersistDocumentInStoreCommandHandler>>();

            var generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            generalConfig.Value.CallerCode = "HBD";


            _handlerContext = new TestableMessageHandlerContext();
            _handler = new PersistDocumentInStoreCommandHandler(generalConfig, _fakeContentServiceApiClient, _dbContext, _fakeDocumentStatusFactory, _fakeDocumentFileLocationFactory, _fakeLogger, _fakeFileSystem);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_When_Primary_Document_Successfully_posted_to_contentService_Then_Document_Status_and_FileLocation_is_Updated()
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
                    _fakeContentServiceApiClient.Post(A<byte[]>.Ignored, Path.GetFileName(message.Filepath)))
                .Returns(Guid.NewGuid());

            // when
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Assert
            A.CallTo(() => _fakeDocumentStatusFactory.GetDocumentStatusProcessor(SourceType.Primary))
                .MustHaveHappened();
            A.CallTo(() => _fakeDocumentFileLocationFactory.GetDocumentFileLocationProcessor(SourceType.Primary))
                .MustHaveHappened();

            A.CallTo(() => _fakeDocumentStatusFactory.GetDocumentStatusProcessor(SourceType.Linked))
                .MustNotHaveHappened();
            A.CallTo(() => _fakeDocumentFileLocationFactory.GetDocumentFileLocationProcessor(SourceType.Linked))
                .MustNotHaveHappened();

            A.CallTo(() => _fakeDocumentStatusFactory.GetDocumentStatusProcessor(SourceType.Database))
                .MustNotHaveHappened();
            A.CallTo(() => _fakeDocumentFileLocationFactory.GetDocumentFileLocationProcessor(SourceType.Database))
                .MustNotHaveHappened();
        }

        [Test]
        public async Task Test_When_Linked_Document_Successfully_posted_to_contentService_Then_Document_Status_and_FileLocation_is_Updated()
        {
            //Given
            var message = new PersistDocumentInStoreCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 5678,
                SourceDocumentId = 1999999,
                SourceType = SourceType.Linked,
                Filepath = @"c:\test\TestImage.tif"
            };

            A.CallTo(() =>
                    _fakeContentServiceApiClient.Post(A<byte[]>.Ignored, Path.GetFileName(message.Filepath)))
                .Returns(Guid.NewGuid());

            // when
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Assert
            A.CallTo(() => _fakeDocumentStatusFactory.GetDocumentStatusProcessor(SourceType.Linked))
                .MustHaveHappened();
            A.CallTo(() => _fakeDocumentFileLocationFactory.GetDocumentFileLocationProcessor(SourceType.Linked))
                .MustHaveHappened();

            A.CallTo(() => _fakeDocumentStatusFactory.GetDocumentStatusProcessor(SourceType.Primary))
                .MustNotHaveHappened();
            A.CallTo(() => _fakeDocumentFileLocationFactory.GetDocumentFileLocationProcessor(SourceType.Primary))
                .MustNotHaveHappened();

            A.CallTo(() => _fakeDocumentStatusFactory.GetDocumentStatusProcessor(SourceType.Database))
                .MustNotHaveHappened();
            A.CallTo(() => _fakeDocumentFileLocationFactory.GetDocumentFileLocationProcessor(SourceType.Database))
                .MustNotHaveHappened();
        }
        
        [Test]
        public async Task Test_When_Database_Document_Successfully_posted_to_contentService_Then_Document_Status_and_FileLocation_is_Updated()
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
                    _fakeContentServiceApiClient.Post(A<byte[]>.Ignored, Path.GetFileName(message.Filepath)))
                .Returns(Guid.NewGuid());

            // when
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Assert
            A.CallTo(() => _fakeDocumentStatusFactory.GetDocumentStatusProcessor(SourceType.Database))
                .MustHaveHappened();
            A.CallTo(() => _fakeDocumentFileLocationFactory.GetDocumentFileLocationProcessor(SourceType.Database))
                .MustHaveHappened();

            A.CallTo(() => _fakeDocumentStatusFactory.GetDocumentStatusProcessor(SourceType.Primary))
                .MustNotHaveHappened();
            A.CallTo(() => _fakeDocumentFileLocationFactory.GetDocumentFileLocationProcessor(SourceType.Primary))
                .MustNotHaveHappened();

            A.CallTo(() => _fakeDocumentStatusFactory.GetDocumentStatusProcessor(SourceType.Linked))
                .MustNotHaveHappened();
            A.CallTo(() => _fakeDocumentFileLocationFactory.GetDocumentFileLocationProcessor(SourceType.Linked))
                .MustNotHaveHappened();
        }

        [Test]
        public void Test_When_Document_does_not_exists_Then_FileNotFoundException_thrown_and_Document_Status_and_FileLocation_is_not_Updated()
        {
            //Given
            var message = new PersistDocumentInStoreCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 5678,
                SourceDocumentId = 1999999,
                SourceType = SourceType.Database,
                Filepath = "DoesNotExists.tif"
            };

            // when
            Assert.ThrowsAsync<FileNotFoundException>(() => _handler.Handle(message, _handlerContext));

            //Assert
            A.CallTo(() => _fakeDocumentStatusFactory.GetDocumentStatusProcessor(A<SourceType>.Ignored))
                .MustNotHaveHappened();
            A.CallTo(() => _fakeDocumentFileLocationFactory.GetDocumentFileLocationProcessor(A<SourceType>.Ignored))
                .MustNotHaveHappened();
        }
        
        [Test]
        public void Test_When_ContentService_Call_fail_Then_Exception_thrown_and_Document_Status_and_FileLocation_is_not_Updated()
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
                    _fakeContentServiceApiClient.Post(A<byte[]>.Ignored, Path.GetFileName(message.Filepath)))
                .Throws(new Exception());

            // when
            Assert.ThrowsAsync<Exception>(() => _handler.Handle(message, _handlerContext));

            //Assert
            A.CallTo(() => _fakeDocumentStatusFactory.GetDocumentStatusProcessor(A<SourceType>.Ignored))
                .MustNotHaveHappened();
            A.CallTo(() => _fakeDocumentFileLocationFactory.GetDocumentFileLocationProcessor(A<SourceType>.Ignored))
                .MustNotHaveHappened();
        }
    }
}

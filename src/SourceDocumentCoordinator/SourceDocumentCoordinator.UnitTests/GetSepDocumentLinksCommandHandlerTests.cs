using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Commands;
using DataServices.Models;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NServiceBus.Testing;
using NUnit.Framework;
using SourceDocumentCoordinator.Handlers;
using SourceDocumentCoordinator.HttpClients;
using WorkflowDatabase.EF;

namespace SourceDocumentCoordinator.UnitTests
{
    public class GetSepDocumentLinksCommandHandlerTests
    {
        private IDataServiceApiClient _fakeDataServiceApiClient;
        private TestableMessageHandlerContext _handlerContext;
        private GetSepDocumentLinksCommandHandler _handler;
        private WorkflowDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();

            _handlerContext = new TestableMessageHandlerContext();
            _handler = new GetSepDocumentLinksCommandHandler(_dbContext, _fakeDataServiceApiClient);
        }

        [TearDown]
        public void CleanUp()
        {
            _dbContext.LinkedDocument.RemoveRange(_dbContext.LinkedDocument.ToArray());
            _dbContext.SaveChanges();
        }


        [Test]
        public async Task Test_expected_linkdocument_saved_to_dbcontext()
        {
            //Given
            var message = new GetSepDocumentLinksCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 5678,
                SourceDocumentId = 1999999
            };

            var assessmentData = new WorkflowDatabase.EF.Models.AssessmentData()
            {
                SdocId = message.SourceDocumentId,
                RsdraNumber = "RSDRA2019000130865"
            };
            await _dbContext.AssessmentData.AddAsync(assessmentData);
            await _dbContext.SaveChangesAsync();

            var documentObjects = new DocumentObjects()
            {
                new DocumentObject()
                {
                    Id = message.SourceDocumentId,
                    SourceName = assessmentData.RsdraNumber,
                    Name = "a very long description"
                }
            };

            A.CallTo(() => _fakeDataServiceApiClient.GetSepDocumentLinks(message.SourceDocumentId)).Returns(documentObjects);

            //When
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Then
            Assert.AreEqual(1, _dbContext.LinkedDocument.Count());
            Assert.AreEqual("SEP", _dbContext.LinkedDocument.First().LinkType);
        }

        [Test]
        public async Task Test_when_no_linkeddocument_nothing_saved_to_db()
        {
            //Given
            var message = new GetSepDocumentLinksCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 1234,
                SourceDocumentId = 1888403
            };

            var assessmentData = new WorkflowDatabase.EF.Models.AssessmentData()
            {
                SdocId = message.SourceDocumentId,
                RsdraNumber = "RSDRA2017000130865"
            };
            await _dbContext.AssessmentData.AddAsync(assessmentData);
            await _dbContext.SaveChangesAsync();

            A.CallTo(() => _fakeDataServiceApiClient.GetSepDocumentLinks(message.SourceDocumentId)).Returns(new DocumentObjects());

            //When
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Then
            CollectionAssert.IsEmpty(_dbContext.LinkedDocument);
        }

    }
}

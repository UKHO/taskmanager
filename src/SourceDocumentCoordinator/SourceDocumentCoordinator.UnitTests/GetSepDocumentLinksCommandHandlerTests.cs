using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Messages.Commands;
using DataServices.Models;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private ILogger<GetSepDocumentLinksCommandHandler> _fakeLogger;
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
            _fakeLogger = A.Dummy<ILogger<GetSepDocumentLinksCommandHandler>>();

            _handlerContext = new TestableMessageHandlerContext();
            _handler = new GetSepDocumentLinksCommandHandler(_dbContext, _fakeDataServiceApiClient, _fakeLogger);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
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
                PrimarySdocId = message.SourceDocumentId,
                RsdraNumber = "RSDRA2019000130865"
            };
            await _dbContext.AssessmentData.AddAsync(assessmentData);
            await _dbContext.SaveChangesAsync();

            var documentObjects = new DocumentObjects()
            {
                new DocumentObject()
                {
                    Id = 9888888,
                    SourceName = assessmentData.RsdraNumber,
                    Name = "a very long description"
                }
            };

            A.CallTo(() => _fakeDataServiceApiClient.GetSepDocumentLinks(message.SourceDocumentId)).Returns(documentObjects);
            
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessmentData(9888888)).Returns(new DocumentAssessmentData()
            {
                SourceName = "RSDRA2019000130872"
            });

            //When
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Then
            Assert.AreEqual(1, _dbContext.LinkedDocument.Count());
            Assert.AreEqual("RSDRA2019000130872", _dbContext.LinkedDocument.First().RsdraNumber);
            Assert.AreEqual(DocumentLinkType.Sep.ToString(), _dbContext.LinkedDocument.First().LinkType);
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
                PrimarySdocId = message.SourceDocumentId,
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


        [Test]
        public void Test_When_some_sep_linked_documents_do_not_have_assessment_data_then_exception_throws_only_for_ones_without_data()
        {

            //Given

            var sepLinkedWithData = 2222;
            var sepLinkedWithoutData = 3333;

            var message = new GetSepDocumentLinksCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 5678,
                SourceDocumentId = 1111
            };

            var docLinks = new DocumentObjects();
            docLinks.AddRange( new List<DocumentObject>()
            {
                new DocumentObject()
                {
                    Id = sepLinkedWithData
                },
                new DocumentObject()
                {
                    Id = sepLinkedWithoutData

                }
            });


            A.CallTo(() => _fakeDataServiceApiClient.GetSepDocumentLinks(message.SourceDocumentId)).Returns(docLinks);

            A.CallTo(() => _fakeDataServiceApiClient.GetAssessmentData(sepLinkedWithData))
                                                                        .Returns(new DocumentAssessmentData()
                                                                        {
                                                                            SdocId = sepLinkedWithData
                                                                        });


            A.CallTo(() => _fakeDataServiceApiClient.GetAssessmentData(sepLinkedWithoutData))
                                                                        .Throws(new ApplicationException($"StatusCode='{HttpStatusCode.InternalServerError}'," +
                                                                                                              $"\n Message= 'No assessment data found for SdocId: {sepLinkedWithoutData}'," +
                                                                                                              $"\n Url=''"));

            //When
            var ex = Assert.ThrowsAsync<ApplicationException>(() => _handler.Handle(message, _handlerContext));

            //Then
            // linked doc without data, exception thrown, not added to DB
            Assert.IsTrue(ex.Message.Contains($"No assessment data found for SdocId: {sepLinkedWithoutData}"));
            Assert.IsFalse(_dbContext.LinkedDocument.Any(l => l.LinkedSdocId == sepLinkedWithoutData));

            // linked doc with data, not included in exception thrown, added to DB
            Assert.IsFalse(ex.Message.Contains($"No assessment data found for SdocId: {sepLinkedWithData}"));
            Assert.AreEqual(1, _dbContext.LinkedDocument.Count());
            Assert.IsTrue(_dbContext.LinkedDocument.Any(l => l.LinkedSdocId == sepLinkedWithData));
            Assert.AreEqual(DocumentLinkType.Sep.ToString(), _dbContext.LinkedDocument.First(l => l.LinkedSdocId == sepLinkedWithData).LinkType);
        }

    }
}

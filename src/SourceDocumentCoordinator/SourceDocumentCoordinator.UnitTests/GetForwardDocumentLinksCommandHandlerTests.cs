﻿using System;
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
    public class GetForwardDocumentLinksCommandHandlerTests
    {
        private IDataServiceApiClient _fakeDataServiceApiClient;
        private ILogger<GetForwardDocumentLinksCommandHandler> _fakeLogger;
        private TestableMessageHandlerContext _handlerContext;
        private GetForwardDocumentLinksCommandHandler _handler;
        private WorkflowDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();
            _fakeLogger = A.Dummy<ILogger<GetForwardDocumentLinksCommandHandler>>();

            _handlerContext = new TestableMessageHandlerContext();
            _handler = new GetForwardDocumentLinksCommandHandler(_dbContext, _fakeDataServiceApiClient, _fakeLogger);
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
            var message = new GetForwardDocumentLinksCommand()
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

            var docLinks = new LinkedDocuments()
            {
                new LinkedDocument()
                {
                    DocId1 = message.SourceDocumentId,
                    DocId2 = 9888888,
                    LinkType = "PARENTCHILD"
                }
            };
            A.CallTo(() => _fakeDataServiceApiClient.GetForwardDocumentLinks(message.SourceDocumentId)).Returns(docLinks);

            A.CallTo(() => _fakeDataServiceApiClient.GetAssessmentData(9888888)).Returns(new DocumentAssessmentData()
            {
                SourceName = "RSDRA2019000130872"
            });

            //When
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Then
            Assert.AreEqual(1, _dbContext.LinkedDocument.Count());
            Assert.AreEqual("RSDRA2019000130872", _dbContext.LinkedDocument.First().RsdraNumber);
            Assert.AreEqual(DocumentLinkType.Forward.ToString(), _dbContext.LinkedDocument.First().LinkType);
        }

        [Test]
        public async Task Test_when_no_linkeddocument_nothing_saved_to_db()
        {
            //Given
            var message = new GetForwardDocumentLinksCommand()
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

            A.CallTo(() => _fakeDataServiceApiClient.GetForwardDocumentLinks(message.SourceDocumentId)).Returns(new LinkedDocuments());

            //When
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Then
            CollectionAssert.IsEmpty(_dbContext.LinkedDocument);
        }


        [Test]
        public void Test_When_some_forward_linked_documents_do_not_have_assessment_data_then_exception_throws_only_for_ones_without_data()
        {

            //Given

            var forwardLinkedWithData = 2222;
            var forwardLinkedWithoutData = 3333;

            var message = new GetForwardDocumentLinksCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 5678,
                SourceDocumentId = 1111
            };

            var docLinks = new LinkedDocuments()
            {
                new LinkedDocument()
                {
                    DocId1 = forwardLinkedWithData,
                    DocId2 = message.SourceDocumentId,
                    LinkType = "PARENTCHILD"
                },
                new LinkedDocument()
                {
                    DocId1 = forwardLinkedWithoutData,
                    DocId2 = message.SourceDocumentId,
                    LinkType = "PARENTCHILD"

                }
            };


            A.CallTo(() => _fakeDataServiceApiClient.GetForwardDocumentLinks(message.SourceDocumentId)).Returns(docLinks);

            A.CallTo(() => _fakeDataServiceApiClient.GetAssessmentData(forwardLinkedWithData))
                                                                        .Returns(new DocumentAssessmentData()
                                                                        {
                                                                            SdocId = forwardLinkedWithData
                                                                        });


            A.CallTo(() => _fakeDataServiceApiClient.GetAssessmentData(forwardLinkedWithoutData))
                                                                        .Throws(new ApplicationException($"StatusCode='{HttpStatusCode.InternalServerError}'," +
                                                                                                              $"\n Message= 'No assessment data found for SdocId: {forwardLinkedWithoutData}'," +
                                                                                                              $"\n Url=''"));

            //When
            var ex = Assert.ThrowsAsync<ApplicationException>(() => _handler.Handle(message, _handlerContext));

            //Then
            // linked doc without data, exception thrown, not added to DB
            Assert.IsTrue(ex.Message.Contains($"No assessment data found for SdocId: {forwardLinkedWithoutData}"));
            Assert.IsFalse(_dbContext.LinkedDocument.Any(l => l.LinkedSdocId == forwardLinkedWithoutData));

            // linked doc with data, not included in exception thrown, added to DB
            Assert.IsFalse(ex.Message.Contains($"No assessment data found for SdocId: {forwardLinkedWithData}"));
            Assert.AreEqual(1, _dbContext.LinkedDocument.Count());
            Assert.IsTrue(_dbContext.LinkedDocument.Any(l => l.LinkedSdocId == forwardLinkedWithData));
            Assert.AreEqual(DocumentLinkType.Forward.ToString(), _dbContext.LinkedDocument.First(l => l.LinkedSdocId == forwardLinkedWithData).LinkType);
        }

    }
}

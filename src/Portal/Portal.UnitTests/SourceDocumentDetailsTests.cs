using System;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class SourceDocumentDetailsTests
    {
        private WorkflowDbContext _dbContext;
        private int ProcessId { get; set; }

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            ProcessId = 123;
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public void Test_InvalidOperationException_thrown_when_no_assessmentdata_exists()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = ProcessId,
                ActivityName = "GregTest",
                AssessmentData = null,
                SerialNumber = "123_sn",
                Status = "Started",
                WorkflowType = "DbAssessment"
            });

            _dbContext.SaveChanges();

            var sourceDocumentDetailsModel = new _SourceDocumentDetailsModel(_dbContext, null, null, null);
            var ex = Assert.Throws<InvalidOperationException>(() =>
                sourceDocumentDetailsModel.OnGet());
            Assert.AreEqual("Unable to retrieve AssessmentData", ex.Data["OurMessage"]);
        }

        [Test]
        public void Test_no_exception_thrown_when_no_primarydocumentstatus_row_exists()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = ProcessId,
                ActivityName = "GregTest",
                AssessmentData = null,
                SerialNumber = "123_sn",
                Status = "Started",
                WorkflowType = "DbAssessment"
            });
            _dbContext.AssessmentData.Add(new AssessmentData
            {
                ProcessId = ProcessId,
                SdocId = 123456,
                SourceDocumentName = "MyName",
                RsdraNumber = "12345",
                ReceiptDate = DateTime.Now,
                EffectiveStartDate = DateTime.Now,
                SourceNature = "Au naturale",
                Datum = "What",
                SourceDocumentType = "This",
                TeamDistributedTo = "HW"
            });
            _dbContext.SaveChanges();

            var sourceDocumentDetailsModel = new _SourceDocumentDetailsModel(_dbContext, null, null, null) { ProcessId = ProcessId };
            Assert.DoesNotThrow(() => sourceDocumentDetailsModel.OnGet());
        }
    }
}

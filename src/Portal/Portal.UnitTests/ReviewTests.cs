using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    [TestFixture]
    public class ReviewTests
    {
        private WorkflowDbContext _dbContext;
        private ReviewModel _reviewModel;
        private int ProcessId { get; set; }

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            ProcessId = 123;

            _reviewModel = new ReviewModel(_dbContext, null, null, null, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_entering_a_primary_sourcetype_that_does_not_exist_results_in_validation_error_message()
        {
            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                AssignedTaskSourceType = "test invalid type"
            };
            await _reviewModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _reviewModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Assign Task 1: Source Type { _reviewModel.PrimaryAssignedTask.AssignedTaskSourceType} does not exist", _reviewModel.ValidationErrorMessages[0]);
        }

        [Test]
        public async Task Test_entering_an_additional_sourcetype_that_does_not_exist_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskSourceType.Add(new AssignedTaskSourceType
            {
                AssignedTaskSourceTypeId = 1,
                Name = "Test entry"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                AssignedTaskSourceType = "Test entry"
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>
            {
                new DbAssessmentAssignTask {ProcessId = ProcessId, AssignedTaskSourceType = "This is invalid"}
            };

            await _reviewModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _reviewModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Additional Assign Task: Invalid Source Type - { _reviewModel.AdditionalAssignedTasks[0].AssignedTaskSourceType}", _reviewModel.ValidationErrorMessages[0]);
        }
    }
}

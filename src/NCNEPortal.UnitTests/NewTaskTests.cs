using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NCNEPortal.Auth;
using NCNEPortal.Calculators;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using NUnit.Framework;

namespace NCNEPortal.UnitTests
{
    [TestFixture]
    public class NewTaskTests
    {
        private NcneWorkflowDbContext _dbContext;
        private NewTaskModel _newTaskModel;
        private ILogger<NewTaskModel> _fakeLogger;
        private INcneUserDbService _fakencneUserDbService;
        private IMilestoneCalculator _milestoneCalculator;
        private IPageValidationHelper _fakePageValidationHelper;
        private IStageTypeFactory _fakeStageTypeFactory;
        private int ProcessId { get; set; }

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<NcneWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;


            _dbContext = new NcneWorkflowDbContext(dbContextOptions);

            ProcessId = 123;

            _fakePageValidationHelper = A.Fake<IPageValidationHelper>();
            _fakeLogger = A.Fake<ILogger<NewTaskModel>>();

            _fakeStageTypeFactory = A.Fake<IStageTypeFactory>();

            _newTaskModel = new NewTaskModel(_dbContext, _milestoneCalculator, _fakeLogger, _fakencneUserDbService, _fakeStageTypeFactory, _fakePageValidationHelper);
        }

        [Test]
        public async Task OnPostSaveAsync_gives_no_validation_errors_when_valid()
        {

            _newTaskModel.Compiler = "Stuart";
            _newTaskModel.ChartType = "Adoption";
            _newTaskModel.WorkflowType = "NE";


            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<List<string>>.Ignored)).Returns(false);
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<List<string>>.Ignored))
                .Invokes(call => call.Arguments.Get<List<string>>("validationErrorMessages").Add("This is an error message"));
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(
                A<TaskRole>.That.Matches(task => task.Compiler == "Stuart"),
                "NE",
                "Adoption",
                A<List<string>>.That.IsEmpty())).Returns(true);

            await _newTaskModel.OnPostSaveAsync();

            Assert.AreEqual(_newTaskModel.ValidationErrorMessages.Count, 0);
        }

        [Test]
        public async Task OnPostSaveAsync_gives_validation_errors_when_invalid()
        {
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<List<string>>.Ignored)).Returns(false);
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<List<string>>.Ignored))
                .Invokes(call => call.Arguments.Get<List<string>>("validationErrorMessages").Add("This is an error message"));

            await _newTaskModel.OnPostSaveAsync();

            Assert.AreEqual(_newTaskModel.ValidationErrorMessages.Count, 1);
            CollectionAssert.Contains(_newTaskModel.ValidationErrorMessages, "This is an error message");
        }

        [Test]
        public async Task OnPostSaveAsync_returns_200_when_valid()
        {
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<List<string>>.Ignored)).Returns(true);

            var result = await _newTaskModel.OnPostSaveAsync();
            var statusCode = (StatusCodeResult)result;

            Assert.AreEqual(200, statusCode.StatusCode);
        }
    }
}

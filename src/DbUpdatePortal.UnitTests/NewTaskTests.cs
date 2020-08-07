using DbUpdatePortal.Auth;
using DbUpdatePortal.Helpers;
using DbUpdatePortal.Pages;
using DbUpdatePortal.UnitTests.Helper;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DbUpdatePortal.UnitTests
{
    public class NewTaskTests
    {
        private DbUpdateWorkflowDbContext _dbContext;
        private NewTaskModel _newTaskModel;
        private ILogger<NewTaskModel> _fakeLogger;
        private IDbUpdateUserDbService _fakencneUserDbService;
        private IPageValidationHelper _fakePageValidationHelper;
        private IStageTypeFactory _fakeStageTypeFactory;


        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<DbUpdateWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;


            _dbContext = new DbUpdateWorkflowDbContext(dbContextOptions);


            _fakePageValidationHelper = A.Fake<IPageValidationHelper>();
            _fakeLogger = A.Fake<ILogger<NewTaskModel>>();
            _fakencneUserDbService = A.Fake<IDbUpdateUserDbService>();

            _fakeStageTypeFactory = A.Fake<IStageTypeFactory>();

            _newTaskModel = new NewTaskModel(_dbContext, _fakeLogger, _fakencneUserDbService, _fakeStageTypeFactory, _fakePageValidationHelper);
        }


        [Test]
        public async Task OnPostSaveAsync_gives_no_validation_errors_when_valid()
        {

            var user = AdUserHelper.CreateTestUser(_dbContext);

            _newTaskModel.Compiler = user;
            _newTaskModel.ChartingArea = "Home waters";
            _newTaskModel.UpdateType = "Steady state";
            _newTaskModel.TaskName = "New Task";
            _newTaskModel.ProductAction = "None";



            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored)).Returns(false);
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored))
                .Invokes(call => call.Arguments.Get<List<string>>("validationErrorMessages").Add("This is an error message"));
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(
                A<TaskRole>.That.Matches(task => task.Compiler == user),
                "New Task",
                "Home waters",
                "Steady state",
                "None",
                A<List<string>>.That.IsEmpty())).Returns(true);
            A.CallTo(() => _fakencneUserDbService.GetAdUserAsync(user.UserPrincipalName)).Returns(user);

            await _newTaskModel.OnPostSaveAsync();

            Assert.AreEqual(_newTaskModel.ValidationErrorMessages.Count, 0);
        }
        [Test]
        public async Task OnPostSaveAsync_gives_validation_errors_when_invalid()
        {
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored)).Returns(false);
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored))
                .Invokes(call => call.Arguments.Get<List<string>>("validationErrorMessages").Add("This is an error message"));

            await _newTaskModel.OnPostSaveAsync();

            Assert.AreEqual(_newTaskModel.ValidationErrorMessages.Count, 1);
            CollectionAssert.Contains(_newTaskModel.ValidationErrorMessages, "This is an error message");
        }
        [Test]
        public async Task OnPostSaveAsync_returns_200_when_valid()
        {

            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored)).Returns(true);


            var result = await _newTaskModel.OnPostSaveAsync();
            var statusCode = (StatusCodeResult)result;

            Assert.AreEqual(200, statusCode.StatusCode);
        }
    }
}

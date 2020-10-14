using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NCNEPortal.Auth;
using NCNEPortal.Calculators;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using NCNEWorkflowDatabase.Tests.Helpers;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NCNEPortal.UnitTests
{
    [TestFixture]
    public class WithdrawalTests
    {
        private NcneWorkflowDbContext _dbContext;
        private WithdrawalModel _withdrawalModel;
        private ILogger<WithdrawalModel> _fakeLogger;
        private INcneUserDbService _fakencneUserDbService;
        private IMilestoneCalculator _milestoneCalculator;
        private IPageValidationHelper _fakePageValidationHelper;
        private IStageTypeFactory _fakeStageTypeFactory;


        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<NcneWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;


            _dbContext = new NcneWorkflowDbContext(dbContextOptions);




            _fakePageValidationHelper = A.Fake<IPageValidationHelper>();
            _fakeLogger = A.Fake<ILogger<WithdrawalModel>>();
            _fakencneUserDbService = A.Fake<INcneUserDbService>();
            _milestoneCalculator = A.Fake<IMilestoneCalculator>();
            _fakeStageTypeFactory = A.Fake<IStageTypeFactory>();

            _withdrawalModel = new WithdrawalModel(_dbContext, _milestoneCalculator, _fakeLogger,
                _fakencneUserDbService,
                _fakeStageTypeFactory, _fakePageValidationHelper);
        }

        [Test]
        public async Task OnPostSaveAsync_gives_no_validation_errors_when_valid()
        {

            var user = AdUserHelper.CreateTestUser(_dbContext);

            _withdrawalModel.Compiler = user;
            _withdrawalModel.ChartType = "Adoption";
            _withdrawalModel.WorkflowType = "Withdrawal";



            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<List<string>>.Ignored)).Returns(false);
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<List<string>>.Ignored))
                .Invokes(call => call.Arguments.Get<List<string>>("validationErrorMessages").Add("This is an error message"));
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(
                A<TaskRole>.That.Matches(task => task.Compiler == user),
                "Withdrawal",
                "Adoption",
                A<List<string>>.That.IsEmpty())).Returns(true);
            A.CallTo(() => _fakencneUserDbService.GetAdUserAsync(user.UserPrincipalName)).Returns(user);

            await _withdrawalModel.OnPostSaveAsync();

            Assert.AreEqual(_withdrawalModel.ValidationErrorMessages.Count, 0);
        }

        [Test]
        public async Task OnPostSaveAsync_gives_validation_errors_when_invalid()
        {
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<List<string>>.Ignored)).Returns(false);
            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<List<string>>.Ignored))
                .Invokes(call => call.Arguments.Get<List<string>>("validationErrorMessages").Add("This is an error message"));

            await _withdrawalModel.OnPostSaveAsync();

            Assert.AreEqual(_withdrawalModel.ValidationErrorMessages.Count, 1);
            CollectionAssert.Contains(_withdrawalModel.ValidationErrorMessages, "This is an error message");
        }

        [Test]
        public async Task OnPostSaveAsync_returns_200_when_valid()
        {
            _dbContext.TaskStageType.AddRange(new TaskStageType()
            {
                AllowRework = false,
                Name = "Withdrawal action",
                SequenceNumber = 5,
                TaskStageTypeId = 19
            }, new TaskStageType()
            {
                AllowRework = false,
                Name = "Consider withdrawn charts",
                SequenceNumber = 19,
                TaskStageTypeId = 18

            },
                new TaskStageType()
                {
                    AllowRework = false,
                    Name = "PMC withdrawal",
                    SequenceNumber = 20,
                    TaskStageTypeId = 20

                }, new TaskStageType()
                {
                    AllowRework = false,
                    Name = "Consider Email SDR",
                    SequenceNumber = 21,
                    TaskStageTypeId = 21

                }
            );

            _dbContext.SaveChanges();

            A.CallTo(() => _fakePageValidationHelper.ValidateNewTaskPage(A<TaskRole>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<List<string>>.Ignored)).Returns(true);

            var result = await _withdrawalModel.OnPostSaveAsync();
            var statusCode = (StatusCodeResult)result;

            Assert.AreEqual(200, statusCode.StatusCode);
        }

    }
}

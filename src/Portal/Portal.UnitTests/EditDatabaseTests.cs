using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Helpers.Auth;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Configuration;
using Portal.Helpers;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class EditDatabaseTests
    {
        private WorkflowDbContext _dbContext;
        private _EditDatabaseModel _editDatabaseModel;
        private ILogger<_EditDatabaseModel> _fakeLogger;
        private IOptions<GeneralConfig> _generalConfig;
        private IAdDirectoryService _fakeAdDirectoryService;
        private ISessionFileGenerator _fakeSessionFileGenerator;
        private ICarisProjectHelper _fakeCarisProjectHelper;
        private ICarisProjectNameGenerator _fakeCarisProjectNameGenerator;
        public int ProcessId { get; set; }

        [SetUp]
        public async Task Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeLogger = A.Dummy<ILogger<_EditDatabaseModel>>();
            _generalConfig = A.Fake<IOptions<GeneralConfig>>();
            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakeSessionFileGenerator = A.Fake<ISessionFileGenerator>();
            _fakeCarisProjectHelper = A.Fake<ICarisProjectHelper>();
            _fakeCarisProjectNameGenerator = A.Fake<ICarisProjectNameGenerator>();

            ProcessId = 123;

            _dbContext.CachedHpdWorkspace.Add(new CachedHpdWorkspace
            {
                Name = "TestWorkspace"
            });
            _dbContext.HpdUser.Add(new HpdUser
            {
                AdUsername = "TestUserAd",
                HpdUsername = "HpdUser"
            });
            await _dbContext.SaveChangesAsync();

            _editDatabaseModel = new _EditDatabaseModel(_dbContext, _fakeLogger, _generalConfig, _fakeAdDirectoryService,
                                                        _fakeSessionFileGenerator, _fakeCarisProjectHelper, _fakeCarisProjectNameGenerator);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_CreateCarisProject_Given_Valid_Data_Then_Updates_DbAssessmentAssessData_WorkspaceAffected()
        {
            //Arrange
            var userWithHpdUserRecord = "TestUserAd";

            var setupAssessData = new DbAssessmentAssessData()
            {
                ProcessId = ProcessId,
                Assessor = userWithHpdUserRecord,
                Verifier = userWithHpdUserRecord
            };
            await _dbContext.DbAssessmentAssessData.AddAsync(setupAssessData);
            await _dbContext.SaveChangesAsync();

            A.CallTo(() => _fakeUserIdentityService.GetFullNameForUser(A<ClaimsPrincipal>.Ignored))
                .Returns(Task.FromResult(userWithHpdUserRecord));

            A.CallTo(() => _fakeCarisProjectHelper.CreateCarisProject(
                    A<int>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<string>.Ignored,
                    A<int>.Ignored))
                .Returns(1);

            //Act
            await _editDatabaseModel.OnPostCreateCarisProjectAsync(ProcessId, "Assess", "TestProject", "TestWorkspace");

            //Assert
            var assessData = await _dbContext.DbAssessmentAssessData.FirstOrDefaultAsync();
            Assert.IsNotNull(assessData);
            Assert.AreEqual("TestWorkspace", assessData.WorkspaceAffected);
            Assert.IsFalse(_dbContext.ChangeTracker.HasChanges());
        }


        [Test]
        public void Test_CreateCarisProject_Throws_InvalidOperationException_When_Invalid_Workspace_Provided()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _editDatabaseModel.OnPostCreateCarisProjectAsync(ProcessId, "Assess", "TestProject", "InvalidWorkspace"));
        }

        [Test]
        public void Test_CreateCarisProject_Throws_InvalidOperationException_When_No_HpdUser_Found()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _editDatabaseModel.OnPostCreateCarisProjectAsync(ProcessId, "Assess", "TestProject", "TestWorkspace"));
        }
    }
}

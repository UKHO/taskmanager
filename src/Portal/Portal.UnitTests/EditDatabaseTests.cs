using System;
using System.Threading.Tasks;
using Common.Helpers;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Auth;
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
        private IUserIdentityService _fakeUserIdentityService;
        private ISessionFileGenerator _fakeSessionFileGenerator;
        private ICarisProjectHelper _fakeCarisProjectHelper;
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
            _fakeUserIdentityService = A.Fake<IUserIdentityService>();
            _fakeSessionFileGenerator = A.Fake<ISessionFileGenerator>();
            _fakeCarisProjectHelper = A.Fake<ICarisProjectHelper>();

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

            _editDatabaseModel = new _EditDatabaseModel(_dbContext, _fakeLogger, _generalConfig, _fakeUserIdentityService, _fakeSessionFileGenerator, _fakeCarisProjectHelper);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
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

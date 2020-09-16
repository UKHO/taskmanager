using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Auth;
using Portal.Configuration;
using Portal.Helpers;
using Portal.UnitTests.Helpers;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class SessionFileGeneratorTests
    {
        private WorkflowDbContext _dbContext;
        private ISessionFileGenerator _sessionFileGenerator;
        private IOptions<SecretsConfig> _secretsConfig;
        private int ProcessId { get; set; }
        private ILogger<SessionFileGenerator> _logger;
        public string WorkspaceAffected { get; set; }
        private IPortalUserDbService _fakePortalUserDbService;

        public AdUser TestUser { get; set; }

        [SetUp]
        public async Task Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);
            TestUser = AdUserHelper.CreateTestUser(_dbContext);

            ProcessId = 123;
            WorkspaceAffected = "TestWorkspace";

            _secretsConfig = A.Fake<IOptions<SecretsConfig>>();
            _secretsConfig.Value.HpdServiceName = "ServiceName";

            _logger = A.Fake<ILogger<SessionFileGenerator>>();

            _fakePortalUserDbService = A.Fake<IPortalUserDbService>();

            _sessionFileGenerator = new SessionFileGenerator(_dbContext,
                _secretsConfig, _logger, _fakePortalUserDbService);

            _dbContext.CachedHpdWorkspace.Add(new CachedHpdWorkspace
            {
                Name = WorkspaceAffected
            });

            await _dbContext.SaveChangesAsync();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_PopulateSessionFile_UserFullName_That_Does_Not_Exist_Throws_InvalidOperationException()
        {
            var hpdUser = new HpdUser()
            {
                HpdUserId = 1,
                AdUser = TestUser,
                HpdUsername = "TestUser1-Caris"
            };
            await _dbContext.HpdUser.AddAsync(hpdUser);

            await _dbContext.SaveChangesAsync();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            Assert.ThrowsAsync(typeof(InvalidOperationException),
                () => _sessionFileGenerator.PopulateSessionFile(
                    ProcessId,
                    "unknownUserEmail",
                    WorkspaceAffected,
                    new CarisProjectDetails(), null, null)
            );
        }

        [Test]
        public async Task Test_PopulateSessionFile_WorkspaceAffected_That_Does_Not_Exist_Throws_InvalidOperationException()
        {
            var hpdUsername = "TestUser1-Caris";
            var hpdUser = new HpdUser()
            {
                HpdUserId = 1,
                AdUser = TestUser,
                HpdUsername = hpdUsername
            };
            await _dbContext.HpdUser.AddAsync(hpdUser);

            await _dbContext.SaveChangesAsync();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            Assert.ThrowsAsync(typeof(InvalidOperationException),
                () => _sessionFileGenerator.PopulateSessionFile(
                    ProcessId,
                    TestUser.UserPrincipalName,
                    "UnknownWorkspaceAffected",
                    new CarisProjectDetails(), null, null)
            );
        }

        [Test]
        public async Task Test_PopulateSessionFile_Returns_Populated_Hpd_Data()
        {
            var hpdUsername = "TestUser1-Caris";
            var hpdUser = new HpdUser()
            {
                HpdUserId = 1,
                AdUser = TestUser,
                HpdUsername = hpdUsername
            };
            await _dbContext.HpdUser.AddAsync(hpdUser);
            await _dbContext.SaveChangesAsync();

            var selectedUsages = new List<string>()
            {
                "Usage1",
                "Usage2"
            };

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            var sessionFile = await _sessionFileGenerator.PopulateSessionFile(
                ProcessId,
                TestUser.UserPrincipalName,
                WorkspaceAffected,
                new CarisProjectDetails(), selectedUsages, null);

            Assert.IsNotNull(sessionFile);
            Assert.IsNotNull(sessionFile.DataSources);
            Assert.AreEqual(hpdUsername,
                sessionFile.DataSources.DataSource[0].SourceParam.USERNAME);
            Assert.AreEqual(hpdUsername,
                sessionFile.DataSources.DataSource[0].SourceParam.ASSIGNED_USER);
            Assert.AreEqual(_secretsConfig.Value.HpdServiceName,
                sessionFile.DataSources.DataSource[0].SourceParam.SERVICENAME);
        }

        [Test]
        public async Task Test_PopulateSessionFile_Returns_Populated_Hpd_Usages()
        {
            var hpdUsername = "TestUser1-Caris";
            var hpdUser = new HpdUser()
            {
                HpdUserId = 1,
                AdUser = TestUser,
                HpdUsername = hpdUsername
            };
            await _dbContext.HpdUser.AddAsync(hpdUser);
            await _dbContext.SaveChangesAsync();

            var selectedUsages = new List<string>()
            {
                "Usage1",
                "Usage2"
            };

            var carisproject = new CarisProjectDetails()
            {
                ProcessId = ProcessId,
                ProjectId = 123456,
                ProjectName = "SomeName",
                Created = DateTime.Today,
                CreatedBy = TestUser
            };

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            var sessionFile = await _sessionFileGenerator.PopulateSessionFile(
                ProcessId,
                TestUser.UserPrincipalName,
                WorkspaceAffected,
                carisproject, selectedUsages, null);

            Assert.IsNotNull(sessionFile);
            Assert.IsNotNull(sessionFile.DataSources);

            Assert.AreEqual(1,
                sessionFile.DataSources.DataSource.Count);

            Assert.AreEqual(selectedUsages[0],
                sessionFile.DataSources.DataSource[0].SourceParam.USAGE);

            Assert.AreEqual($":HPD:Project:|{carisproject.ProjectName}",
                sessionFile.DataSources.DataSource[0].SourceString);
            Assert.That(sessionFile.DataSources.DataSource[0].SourceParam.SELECTEDPROJECTUSAGES.Value, Is.EqualTo(selectedUsages));


            Assert.IsNotNull(sessionFile.Views);

            Assert.AreEqual($":HPD:Project:|{carisproject.ProjectName}:{selectedUsages[0]}",
                sessionFile.Views.View.DisplayState.DisplayLayer.Name);
        }

        [Test]
        public async Task Test_PopulateSessionFile_Returns_Populated_WorkspaceAffected()
        {
            var hpdUsername = "TestUser1-Caris";
            var hpdUser = new HpdUser()
            {
                HpdUserId = 1,
                AdUser = TestUser,
                HpdUsername = hpdUsername
            };
            await _dbContext.HpdUser.AddAsync(hpdUser);
            await _dbContext.SaveChangesAsync();

            var selectedUsages = new List<string>()
            {
                "Usage1",
                "Usage2"
            };

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            var sessionFile = await _sessionFileGenerator.PopulateSessionFile(
                ProcessId,
                TestUser.UserPrincipalName,
                WorkspaceAffected,
                new CarisProjectDetails(), selectedUsages, null);

            Assert.IsNotNull(sessionFile);
            Assert.IsNotNull(sessionFile.DataSources);
            Assert.AreEqual(WorkspaceAffected, sessionFile.DataSources.DataSource[0].SourceParam.WORKSPACE);
        }

        [Test]
        public async Task Test_PopulateSessionFile_Returns_Populated_With_Selected_Sources()
        {
            var hpdUsername = "TestUser1-Caris";
            var hpdUser = new HpdUser()
            {
                HpdUserId = 1,
                AdUser = TestUser,
                HpdUsername = hpdUsername
            };
            await _dbContext.HpdUser.AddAsync(hpdUser);
            await _dbContext.SaveChangesAsync();

            var selectedUsages = new List<string>()
            {
                "Usage1",
                "Usage2"
            };

            var selectedSources = new List<string>()
            {
                "c:\\Temp\\Test1.tif",
                "c:\\Temp\\Test2.tif"
            };

            var carisproject = new CarisProjectDetails()
            {
                ProcessId = ProcessId,
                ProjectId = 123456,
                ProjectName = "SomeName",
                Created = DateTime.Today,
                CreatedBy = TestUser
            };

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            var sessionFile = await _sessionFileGenerator.PopulateSessionFile(
                ProcessId,
                TestUser.UserPrincipalName,
                WorkspaceAffected,
                carisproject, selectedUsages, selectedSources);

            Assert.IsNotNull(sessionFile);
            Assert.IsNotNull(sessionFile.DataSources);

            Assert.AreEqual(3,
                sessionFile.DataSources.DataSource.Count);

            Assert.AreEqual(
                        $":HPD:Project:|{carisproject.ProjectName}",
                        sessionFile.DataSources.DataSource[0].SourceString);
            Assert.AreEqual(
                        selectedSources[0],
                        sessionFile.DataSources.DataSource[1].SourceString);
            Assert.AreEqual(
                        selectedSources[1],
                        sessionFile.DataSources.DataSource[2].SourceString);

            Assert.AreEqual(
                Path.GetFileNameWithoutExtension(selectedSources[0]),
                sessionFile.DataSources.DataSource[1].SourceParam.DisplayName.Value);
            Assert.AreEqual(
                Path.GetFileNameWithoutExtension(selectedSources[1]),
                sessionFile.DataSources.DataSource[2].SourceParam.DisplayName.Value);

            Assert.AreEqual(
                selectedSources[0],
                sessionFile.DataSources.DataSource[1].SourceParam.SurfaceString.Value);
            Assert.AreEqual(
                selectedSources[1],
                sessionFile.DataSources.DataSource[2].SourceParam.SurfaceString.Value);
        }
    }
}

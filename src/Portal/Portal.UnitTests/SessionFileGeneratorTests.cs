using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Configuration;
using Portal.Helpers;
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
        private string UserFullName { get; set; }
        private ILogger<SessionFileGenerator> _logger;
        public string WorkspaceAffected { get; set; }

        [SetUp]
        public async Task Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            ProcessId = 123;
            WorkspaceAffected = "TestWorkspace";
            UserFullName = "TestUser";

            _secretsConfig = A.Fake<IOptions<SecretsConfig>>();
            _secretsConfig.Value.HpdServiceName = "ServiceName";

            _logger = A.Fake<ILogger<SessionFileGenerator>>();

            _sessionFileGenerator = new SessionFileGenerator(_dbContext,
                _secretsConfig, _logger);

            var assessData = new DbAssessmentAssessData
            {
                ProcessId = 123,
                WorkspaceAffected = "TestWorkspace"
            };
            _dbContext.DbAssessmentAssessData.Add(assessData);
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
            UserFullName = "DOES NOT EXIST";
            var hpdUser = new HpdUser()
            {
                HpdUserId = 1,
                AdUsername = "Test User 1",
                HpdUsername = "TestUser1-Caris"
            };
            await _dbContext.HpdUser.AddAsync(hpdUser);

            await _dbContext.SaveChangesAsync();

            Assert.ThrowsAsync(typeof(InvalidOperationException),
                () => _sessionFileGenerator.PopulateSessionFile(
                    ProcessId,
                    UserFullName,
                    "Assess",
                    new CarisProjectDetails(), null, null)
            );
        }

        [Test]
        public async Task Test_PopulateSessionFile_Returns_Populated_Hpd_Data()
        {
            UserFullName = "Test User 1";
            var hpdUsername = "TestUser1-Caris";
            var hpdUser = new HpdUser()
            {
                HpdUserId = 1,
                AdUsername = UserFullName,
                HpdUsername = hpdUsername
            };
            await _dbContext.HpdUser.AddAsync(hpdUser);
            await _dbContext.SaveChangesAsync();

            var selectedUsages = new List<string>()
            {
                "Usage1",
                "Usage2"
            };

            var sessionFile = await _sessionFileGenerator.PopulateSessionFile(
                ProcessId,
                UserFullName,
                "Assess",
                new CarisProjectDetails(), selectedUsages, null);

            Assert.IsNotNull(sessionFile);
            Assert.IsNotNull(sessionFile.DataSources);
            Assert.AreEqual(hpdUsername,
                sessionFile.DataSources.DataSource.SourceParam.USERNAME);
            Assert.AreEqual(hpdUsername,
                sessionFile.DataSources.DataSource.SourceParam.ASSIGNED_USER);
            Assert.AreEqual(_secretsConfig.Value.HpdServiceName,
                sessionFile.DataSources.DataSource.SourceParam.SERVICENAME);
        }
        
        [Test]
        public async Task Test_PopulateSessionFile_Returns_Populated_Hpd_Usages()
        {
            UserFullName = "Test User 1";
            var hpdUsername = "TestUser1-Caris";
            var hpdUser = new HpdUser()
            {
                HpdUserId = 1,
                AdUsername = UserFullName,
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
                CreatedBy = UserFullName
            };

            var sessionFile = await _sessionFileGenerator.PopulateSessionFile(
                ProcessId,
                UserFullName,
                "Assess",
                carisproject, selectedUsages, null);

            Assert.IsNotNull(sessionFile);
            Assert.IsNotNull(sessionFile.DataSources);
            Assert.AreEqual(selectedUsages[0],
                sessionFile.DataSources.DataSource.SourceParam.USAGE);

            Assert.AreEqual($":HPD:Project:|{carisproject.ProjectName}",
                sessionFile.DataSources.DataSource.SourceString);
            Assert.That(sessionFile.DataSources.DataSource.SourceParam.SELECTEDPROJECTUSAGES.Value, Is.EqualTo(selectedUsages));
        }

        [Test]
        public async Task Test_PopulateSessionFile_Returns_Populated_WorkspaceAffected()
        {
            UserFullName = "Test User 1";
            var hpdUsername = "TestUser1-Caris";
            var hpdUser = new HpdUser()
            {
                HpdUserId = 1,
                AdUsername = UserFullName,
                HpdUsername = hpdUsername
            };
            await _dbContext.HpdUser.AddAsync(hpdUser);
            await _dbContext.SaveChangesAsync();


            var selectedUsages = new List<string>()
            {
                "Usage1",
                "Usage2"
            };

            var sessionFile = await _sessionFileGenerator.PopulateSessionFile(
                ProcessId,
                UserFullName,
                "Assess",
                new CarisProjectDetails(), selectedUsages, null);

            Assert.IsNotNull(sessionFile);
            Assert.IsNotNull(sessionFile.DataSources);
            Assert.AreEqual("TestWorkspace", sessionFile.DataSources.DataSource.SourceParam.WORKSPACE);
        }
    }
}

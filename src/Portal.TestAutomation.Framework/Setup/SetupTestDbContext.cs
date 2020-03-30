using BoDi;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Portal.TestAutomation.Framework.Configuration;
using TechTalk.SpecFlow;
using WorkflowDatabase.EF;

namespace Portal.TestAutomation.Framework.Setup
{
    [Binding]
    internal sealed class SetupTestDbContext
    {
        private readonly IObjectContainer _objectContainer;
        private WorkflowDbContext _workflowDbContext;

        public SetupTestDbContext(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario(Order = 50)]
        public void InitializeDbContext()
        {
            var dbConfig = _objectContainer.Resolve<DbConfig>();
            var secrets = _objectContainer.Resolve<SecretsConfig>();

            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(ConfigHelpers.IsLocalDevelopment,
                ConfigHelpers.IsAzureDevOpsBuild || ConfigHelpers.IsAzure || ConfigHelpers.IsAzureDevelopment
                    ? dbConfig.WorkflowDbServer
                    : dbConfig.LocalDbServer, dbConfig.WorkflowDbName,
                dbConfig.WorkflowDbUITestAcct, secrets.WorkflowDbPassword);

            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlServer(workflowDbConnectionString)
                .Options;

            _workflowDbContext = new WorkflowDbContext(dbContextOptions);

            _objectContainer.RegisterInstanceAs(_workflowDbContext);
        }

//        [BeforeScenario(Order = 100)]
        public void SeedDatabase()
        {
            TestWorkflowDatabaseSeeder.UsingDbContext(_workflowDbContext).PopulateTables().SaveChanges();
        }
    }
}
using BoDi;
using Common.Helpers;
using Common.TestAutomation.Framework.Configs;
using Common.TestAutomation.Framework.Logging;
using Common.TestAutomation.Framework.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Portal.TestAutomation.Framework.Configuration;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;
using WorkflowDatabase.EF;

namespace Portal.TestAutomation.Framework.Setup
{
    [Binding]
    public class ConfigSupport
    {
        private static SecretsConfig _secrets;
        private static UrlsConfig _urls;
        private static DbConfig _dbConfig;
        private static WorkflowDbContext _workflowDbContext;

        private readonly IObjectContainer _objectContainer;

        public ConfigSupport(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeTestRun(Order = 1)]
        public static void PopulateConfigsFromAzure()
        {
            var appConfigRoot = AzureAppConfigConfigurationRoot.Instance;
            var keyVaultRoot = AzureKeyVaultConfigConfigurationRoot.Instance;

            _secrets = new SecretsConfig();
            _urls = new UrlsConfig();
            _dbConfig = new DbConfig();

            appConfigRoot.GetSection("databases").Bind(_dbConfig);
            appConfigRoot.GetSection("urls").Bind(_dbConfig);
            appConfigRoot.GetSection("urls").Bind(_urls);

            keyVaultRoot.GetSection("PortalUITest").Bind(_secrets);
            keyVaultRoot.GetSection("PortalUITest").Bind(_dbConfig);
        }

        [BeforeTestRun]
        public static void SetupDatabase()
        {
            InitialiseWorkflowDbContext();

            TestWorkflowDatabaseSeeder.UsingDbContext(_workflowDbContext).PopulateTables().SaveChanges();

            TestData.UsingDbContext(_workflowDbContext)
                .AddUser(_secrets.LoginAccount)
                .ReassignReviewsToUser(_secrets.LoginAccount)
                .SaveChanges();
        }

        private static void InitialiseWorkflowDbContext()
        {
            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(ConfigHelpers.IsLocalDevelopment,
                ConfigHelpers.IsAzureDevOpsBuild || ConfigHelpers.IsAzure || ConfigHelpers.IsAzureDevelopment
                    ? _dbConfig.WorkflowDbServer
                    : _dbConfig.LocalDbServer, _dbConfig.WorkflowDbName,
                _dbConfig.WorkflowDbUITestAcct, _dbConfig.WorkflowDbPassword);

            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlServer(workflowDbConnectionString)
                .Options;

            _workflowDbContext = new WorkflowDbContext(dbContextOptions);
        }


        [BeforeScenario(Order = 1)]
        public void RegisterConfigs()
        {
            _objectContainer.RegisterInstanceAs(_secrets);
            _objectContainer.RegisterInstanceAs(_urls);
            _objectContainer.RegisterInstanceAs(_dbConfig);
            _objectContainer.RegisterInstanceAs(_workflowDbContext);
        }

        [BeforeScenario(Order = 5)]
        public void RegisterSpecFlowLogger()
        {
            _objectContainer.RegisterTypeAs<SpecFlowLogging, ITestLogging>();
        }

        [BeforeScenario(Order = 19)]
        public void RegisterLandingPage()
        {
            _objectContainer.RegisterTypeAs<LandingPage, ILandingPage>();
        }
    }
}
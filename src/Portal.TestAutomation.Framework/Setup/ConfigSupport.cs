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

        [BeforeScenario(Order = 1)]
        public void RegisterAzureConfigs()
        {
            _objectContainer.RegisterInstanceAs(_secrets);
            _objectContainer.RegisterInstanceAs(_urls);
        }

        [BeforeTestRun]
        public static void SetupDatabase()
        {

            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(ConfigHelpers.IsLocalDevelopment,
                ConfigHelpers.IsAzureDevOpsBuild || ConfigHelpers.IsAzure || ConfigHelpers.IsAzureDev
                    ? _dbConfig.WorkflowDbServer
                    : _dbConfig.LocalDbServer, _dbConfig.WorkflowDbName,
                _dbConfig.WorkflowDbUITestAcct, _dbConfig.WorkflowDbPassword);

            InitialiseWorkflowDbContext(workflowDbConnectionString);

            TestWorkflowDatabaseSeeder.UsingDbConnectionString(workflowDbConnectionString).PopulateTables().SaveChanges();

            TestData.UsingDbConnectionString(workflowDbConnectionString)
                .AddUser(_secrets.LoginAccount)
                .AssignTasksToCurrentUser(_secrets.LoginAccount);
        }

        private static void InitialiseWorkflowDbContext(string workflowDbConnectionString)
        {

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
            _objectContainer.RegisterTypeAs<LandingPage, IPage>();
        }
    }
}
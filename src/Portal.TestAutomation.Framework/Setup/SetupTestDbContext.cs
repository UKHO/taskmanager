using BoDi;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Portal.TestAutomation.Framework.Configuration;
using Portal.TestAutomation.Framework.Setup;
using TechTalk.SpecFlow;
using WorkflowDatabase.EF;

namespace Portal.TestAutomation.Framework.Driver
{
    [Binding]
    internal sealed class SetupTestDbContext
    {
        private readonly IObjectContainer _objectContainer;

        public SetupTestDbContext(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario(Order = 1)]
        public void InitializeDbContext()
        {
            var config = SetupConfig.GetAndBindDbConfig();

            // Get a ready-to-use Key Vault client
            var (keyVaultAddress, keyVaultClient) = SecretsHelpers.SetUpKeyVaultClient();

            // Populate SecretsConfig using the setup class
            var secrets = SetupConfig.GetAndBindSecretsConfig(keyVaultAddress, keyVaultClient);

            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(ConfigHelpers.IsLocalDevelopment,
                ConfigHelpers.IsAzureDevOpsBuild ? config.WorkflowDbServer : config.LocalDbServer, config.WorkflowDbName,
                config.WorkflowDbUITestAcct, secrets.Result.WorkflowDbPassword);

            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlServer(workflowDbConnectionString)
                .Options;

            var dbContext = new WorkflowDbContext(dbContextOptions);

            _objectContainer.RegisterInstanceAs<WorkflowDbContext>(dbContext);
        }

    }
}
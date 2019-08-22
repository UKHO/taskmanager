using System;
using BoDi;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Portal.TestAutomation.Framework.Configuration;
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
            var config = new DbConfig();
     
            var configRoot = AzureAppConfigConfigurationRoot.Instance;
            configRoot.GetSection("databases").Bind(config);
            configRoot.GetSection("urls").Bind(config);

            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(ConfigHelpers.IsLocalDevelopment,
                ConfigHelpers.IsLocalDevelopment ? config.LocalDbServer : config.WorkflowDbServer, config.WorkflowDbName);

            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlServer(workflowDbConnectionString)
                .Options;

            var dbContext = new WorkflowDbContext(dbContextOptions);

            _objectContainer.RegisterInstanceAs<WorkflowDbContext>(dbContext);
        }

    }
}
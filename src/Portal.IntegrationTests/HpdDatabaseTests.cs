using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using HpdDatabase.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Portal.Configuration;
namespace Portal.IntegrationTests
{
    public class HpdDatabaseTests
    {
        private HpdDbContext _dbContext;
        private DbContextOptions<HpdDbContext> _dbContextOptions;

        [SetUp]
        public async Task Setup()
        {
            var keyVaultConfigRoot = AzureKeyVaultConfigConfigurationRoot.Instance;
            var config = SetStartupSecretsConfig(keyVaultConfigRoot);
            var connection = DatabasesHelpers.BuildOracleConnectionString(config.DataSource,
                config.UserId, config.Password);
            _dbContextOptions = new DbContextOptionsBuilder<HpdDbContext>()
                .UseOracle(connection)
                .Options;
            _dbContext = new HpdDbContext(_dbContextOptions);
            _dbContext.Database.OpenConnection();
            var helper = new CarisProjectHelper(_dbContext);
            var users = new List<string>()
            {
                "MILLARDS_CARIS",
                "PARKERD_CARIS",
                "TALBOTA_CARIS",
                "YARDP_CARIS",
                "ROSSITERA_CARIS",
                "RIGGK_CARIS"
            };
            try
            {
                await helper.CreateCarisProject(7777, "SamirTesting", "WORTHG_CARIS", users, "New Source", "New", "Normal", 300, "3274_0");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        [Test]
        public void Test_Hpd_Db_Connection()
        {
            var retrievedData = _dbContext.CarisProjectData.Count();
            Assert.Greater(retrievedData, 0);
        }
        private StartupSecretsConfig SetStartupSecretsConfig(IConfigurationRoot keyVaultConfigRoot)
        {
            var startupSecretsConfig = new StartupSecretsConfig();
            keyVaultConfigRoot.GetSection("HpdDbSection").Bind(startupSecretsConfig);
            return startupSecretsConfig;
        }
    }
}
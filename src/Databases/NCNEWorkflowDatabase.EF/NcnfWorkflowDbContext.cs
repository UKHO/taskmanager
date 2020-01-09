using System;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NCNEWorkflowDatabase.EF.Models;

namespace NCNEWorkflowDatabase.EF
{
    public class NcnfWorkflowDbContext : DbContext
    {
        public NcnfWorkflowDbContext(DbContextOptions<NcnfWorkflowDbContext> options)
            : base(options)
        {
            var environmentName = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "";
            var isLocalDevelopment = environmentName.Equals("LocalDevelopment", StringComparison.OrdinalIgnoreCase);
            var azureDevOpsBuild = environmentName.Equals("AzureDevOpsBuild", StringComparison.OrdinalIgnoreCase);

            if (!isLocalDevelopment && !azureDevOpsBuild)
            {
               // (this.Database.GetDbConnection() as SqlConnection).AccessToken = new AzureServiceTokenProvider().GetAccessTokenAsync("https://database.windows.net/").Result;
            }
        }

        public DbSet<NcneTaskInfo> NcneTaskData { get; set; }
       
    }
}
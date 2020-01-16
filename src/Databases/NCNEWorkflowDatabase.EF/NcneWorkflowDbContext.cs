using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using NCNEWorkflowDatabase.EF.Models;
using System;
using System.Data.SqlClient;

namespace NCNEWorkflowDatabase.EF
{
    public class NcneWorkflowDbContext : DbContext
    {
        public NcneWorkflowDbContext(DbContextOptions<NcneWorkflowDbContext> options)
            : base(options)
        {
            var environmentName = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "";
            var isLocalDevelopment = environmentName.Equals("LocalDevelopment", StringComparison.OrdinalIgnoreCase);
            var azureDevOpsBuild = environmentName.Equals("AzureDevOpsBuild", StringComparison.OrdinalIgnoreCase);

            if (!isLocalDevelopment && !azureDevOpsBuild)
            {
                (this.Database.GetDbConnection() as SqlConnection).AccessToken = new AzureServiceTokenProvider().GetAccessTokenAsync("https://database.windows.net/").Result;
            }
        }

        public DbSet<NcneTaskInfo> NcneTaskInfo { get; set; }
        public DbSet<NcneTaskNote> NcneTaskNote { get; set; }

    }
}
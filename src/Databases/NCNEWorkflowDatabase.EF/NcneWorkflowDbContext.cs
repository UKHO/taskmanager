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

        public DbSet<TaskInfo> TaskInfo { get; set; }
        public DbSet<TaskNote> TaskNote { get; set; }
        public DbSet<TaskRole> TaskRole { get; set; }
        public DbSet<TaskStage> TaskStage { get; set; }

        public DbSet<TaskComment> TaskComment { get; set; }
        public DbSet<TaskStageComment> TaskStageComment { get; set; }
        public DbSet<TaskStageType> TaskStageType { get; set; }

        public DbSet<ChartType> ChartType { get; set; }
        public DbSet<WorkflowType> WorkflowType { get; set; }
        public DbSet<CarisProjectDetails> CarisProjectDetails { get; set; }

        public DbSet<HpdUser> HpdUser { get; set; }
        public DbSet<AdUser> AdUser { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskStage>()
                .HasKey(o => new { o.ProcessId, o.TaskStageId });


            modelBuilder.Entity<TaskRole>().HasIndex(i => i.ProcessId).IsUnique();
            modelBuilder.Entity<TaskStage>().Ignore(l => l.IsReadOnly);
        }
    }
}
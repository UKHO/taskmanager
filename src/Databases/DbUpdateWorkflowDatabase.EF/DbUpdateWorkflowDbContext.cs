using System;
using DbUpdateWorkflowDatabase.EF.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DbUpdateWorkflowDatabase.EF
{
    public class DbUpdateWorkflowDbContext : DbContext
    {
        public DbUpdateWorkflowDbContext(DbContextOptions<DbUpdateWorkflowDbContext> options) : base(options)
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

        public DbSet<ChartingArea> ChartingArea { get; set; }
        public DbSet<UpdateType> UpdateType { get; set; }
        public DbSet<CarisProjectDetails> CarisProjectDetails { get; set; }

        public DbSet<HpdUser> HpdUser { get; set; }
        public DbSet<AdUser> AdUser { get; set; }

        public DbSet<ProductAction> ProductAction { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskInfo>()
                .HasMany(x => x.TaskComment)
                .WithOne()
                .HasPrincipalKey(p => p.ProcessId)
                .HasForeignKey(p => p.ProcessId);


            modelBuilder.Entity<TaskInfo>()
                .HasMany(x => x.TaskStage)
                .WithOne()
                .HasPrincipalKey(p => p.ProcessId)
                .HasForeignKey(p => p.ProcessId);

            modelBuilder.Entity<TaskStage>()
                .HasMany(x => x.TaskStageComment)
                .WithOne()
                .HasPrincipalKey(p => new { p.ProcessId, p.TaskStageId })
                .HasForeignKey(p => new { p.ProcessId, p.TaskStageId });


            modelBuilder.Entity<TaskInfo>()
                .HasOne(x => x.TaskRole)
                .WithOne()
                .HasForeignKey<TaskRole>(r => r.ProcessId);

            modelBuilder.Entity<TaskInfo>()
                .HasOne(n => n.TaskNote)
                .WithOne()
                .HasForeignKey<TaskNote>(n => n.ProcessId);


            modelBuilder.Entity<TaskStage>()
                .HasKey(o => new { o.ProcessId, o.TaskStageId });


            modelBuilder.Entity<TaskRole>().HasIndex(i => i.ProcessId).IsUnique();
            modelBuilder.Entity<TaskStage>().Ignore(l => l.IsReadOnly);
        }
    }
}

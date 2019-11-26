using System;
using System.Data.SqlClient;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF.Models;

namespace WorkflowDatabase.EF
{
    public class WorkflowDbContext : DbContext
    {
        public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
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

        public DbSet<AssessmentData> AssessmentData { get; set; }
        public DbSet<Comments> Comment { get; set; }
        public DbSet<DbAssessmentReviewData> DbAssessmentReviewData { get; set; }
        public DbSet<WorkflowInstance> WorkflowInstance { get; set; }
        public DbSet<PrimaryDocumentStatus> PrimaryDocumentStatus { get; set; }
        public DbSet<DatabaseDocumentStatus> DatabaseDocumentStatus { get; set; }
        public DbSet<LinkedDocuments> LinkedDocument { get; set; }
        public DbSet<OnHold> OnHold { get; set; }
        public DbSet<TaskNote> TaskNote { get; set; }
        public DbSet<HpdUsages> HpdUsage { get; set; }
        public DbSet<DataImpact> DataImpact { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkflowInstance>().HasKey(x => x.WorkflowInstanceId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.Comment);

            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.OnHold);

            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.LinkedDocument)
                .WithOne()
                .HasPrincipalKey(p => p.ProcessId)
                .HasForeignKey(p => p.ProcessId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(x => x.PrimaryDocumentStatus)
                .WithOne()
                .HasPrincipalKey<WorkflowInstance>(p => p.ProcessId)
                .HasForeignKey<PrimaryDocumentStatus>(p => p.ProcessId);


            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.DatabaseDocumentStatus)
                .WithOne()
                .HasPrincipalKey(p => p.ProcessId)
                .HasForeignKey(p => p.ProcessId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.DataImpact)
                .WithOne()
                .HasPrincipalKey(p => p.ProcessId)
                .HasForeignKey(p => p.ProcessId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(x => x.DbAssessmentReviewData);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(p => p.AssessmentData)
                .WithOne()
                .HasPrincipalKey<WorkflowInstance>(p => p.ProcessId)
                .HasForeignKey<AssessmentData>(p => p.ProcessId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(x => x.TaskNote);

            modelBuilder.Entity<DataImpact>()
                .HasOne(x => x.HpdUsages)
                .WithOne()
                .HasPrincipalKey<DataImpact>(p => p.HpdUsageId)
                .HasForeignKey<HpdUsages>(p => p.HpdUsagesId);

            modelBuilder.Entity<Comments>().HasKey(x => x.CommentId);

            modelBuilder.Entity<LinkedDocuments>().HasKey(x => x.LinkedDocumentId);

            modelBuilder.Entity<PrimaryDocumentStatus>().HasKey(x => x.PrimaryDocumentStatusId);

            modelBuilder.Entity<DatabaseDocumentStatus>().HasKey(x => x.DatabaseDocumentStatusId);

            modelBuilder.Entity<AssessmentData>().HasKey(x => x.AssessmentDataId);

            modelBuilder.Entity<OnHold>().HasKey(x => x.OnHoldId);
            modelBuilder.Entity<TaskNote>().HasKey(x => x.TaskNoteId);

            modelBuilder.Entity<HpdUsages>().HasKey(x => x.HpdUsagesId);

            modelBuilder.Entity<DataImpact>().HasKey(x => x.DataImpactId);

            modelBuilder.Entity<LinkedDocuments>().Ignore(l => l.ContentServiceUri);
            modelBuilder.Entity<DatabaseDocumentStatus>().Ignore(l => l.ContentServiceUri);

            base.OnModelCreating(modelBuilder);
        }
    }
}
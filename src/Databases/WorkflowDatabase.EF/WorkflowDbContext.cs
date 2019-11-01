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

            if (!isLocalDevelopment)
            {
                (this.Database.GetDbConnection() as SqlConnection).AccessToken = new AzureServiceTokenProvider().GetAccessTokenAsync("https://database.windows.net/").Result;
            }
        }

        public DbSet<AssessmentData> AssessmentData { get; set; }
        public DbSet<Comments> Comment { get; set; }
        public DbSet<DbAssessmentReviewData> DbAssessmentReviewData { get; set; }
        public DbSet<WorkflowInstance> WorkflowInstance { get; set; }
        public DbSet<PrimaryDocumentStatus> PrimaryDocumentStatus { get; set; }
        public DbSet<LinkedDocument> LinkedDocument { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkflowInstance>().HasKey(x => x.WorkflowInstanceId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.Comment);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(x => x.PrimaryDocumentStatus)
                .WithOne()
                .HasPrincipalKey<WorkflowInstance>(p => p.ProcessId)
                .HasForeignKey<PrimaryDocumentStatus>(p => p.ProcessId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(x => x.DbAssessmentReviewData);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(p => p.AssessmentData)
                .WithOne()
                .HasPrincipalKey<WorkflowInstance>(p => p.ProcessId)
                .HasForeignKey<AssessmentData>(p => p.ProcessId);

            modelBuilder.Entity<Comments>().HasKey(x => x.CommentId);

            modelBuilder.Entity<PrimaryDocumentStatus>().HasKey(x => x.PrimaryDocumentStatusId);

            modelBuilder.Entity<AssessmentData>().HasKey(x => x.AssessmentDataId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.LinkedDocument)
                .WithOne()
                .HasPrincipalKey(w => w.ProcessId)
                .HasForeignKey(l => l.ProcessId);

            modelBuilder.Entity<LinkedDocument>().HasKey(x => x.LinkedDocumentId);
            modelBuilder.Entity<LinkedDocument>().Ignore(l => l.ContentServiceUri);

            base.OnModelCreating(modelBuilder);
        }
    }
}
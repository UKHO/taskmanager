using System;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF.Models;

namespace WorkflowDatabase.EF
{
    public class WorkflowDbContext : DbContext
    {
        public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
            : base(options)
        {
            if (!Database.IsSqlServer()) return;

            if (Database.GetDbConnection() is SqlConnection dbConnection)
            {
                dbConnection.AccessToken = new AzureServiceTokenProvider()
                    .GetAccessTokenAsync("https://database.windows.net/").Result;
            }
            else
            {
                throw new ApplicationException("Could not configure Db AccessToken as the DbConnection is null");
            }
        }

        public DbSet<AssessmentData> AssessmentData { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<DbAssessmentReviewData> DbAssessmentReviewData { get; set; }
        public DbSet<DbAssessmentAssessData> DbAssessmentAssessData { get; set; }
        public DbSet<DbAssessmentVerifyData> DbAssessmentVerifyData { get; set; }
        public DbSet<WorkflowInstance> WorkflowInstance { get; set; }
        public DbSet<PrimaryDocumentStatus> PrimaryDocumentStatus { get; set; }
        public DbSet<DatabaseDocumentStatus> DatabaseDocumentStatus { get; set; }
        public DbSet<LinkedDocument> LinkedDocument { get; set; }
        public DbSet<OnHold> OnHold { get; set; }
        public DbSet<TaskNote> TaskNote { get; set; }
        public DbSet<HpdUsage> HpdUsage { get; set; }
        public DbSet<ProductAction> ProductAction { get; set; }
        public DbSet<ProductActionType> ProductActionType { get; set; }
        public DbSet<SncAction> SncAction { get; set; }
        public DbSet<SncActionType> SncActionType { get; set; }
        public DbSet<DbAssessmentAssignTask> DbAssessmentAssignTask { get; set; }
        public DbSet<AssignedTaskType> AssignedTaskType { get; set; }
        public DbSet<DataImpact> DataImpact { get; set; }
        public DbSet<HpdUser> HpdUser { get; set; }
        public DbSet<AdUser> AdUsers { get; set; }
        public DbSet<CachedHpdWorkspace> CachedHpdWorkspace { get; set; }
        public DbSet<CachedHpdEncProduct> CachedHpdEncProduct { get; set; }
        public DbSet<CarisProjectDetails> CarisProjectDetails { get; set; }
        public DbSet<OpenAssessmentsQueue> OpenAssessmentsQueue { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.LinkedDocument)
                .WithOne()
                .HasPrincipalKey(p => p.ProcessId)
                .HasForeignKey(p => p.ProcessId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.DbAssessmentAssignTask)
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
                .HasMany(x => x.ProductAction)
                .WithOne()
                .HasPrincipalKey(p => p.ProcessId)
                .HasForeignKey(p => p.ProcessId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.SncAction)
                .WithOne()
                .HasPrincipalKey(p => p.ProcessId)
                .HasForeignKey(p => p.ProcessId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(p => p.AssessmentData)
                .WithOne()
                .HasPrincipalKey<WorkflowInstance>(p => p.ProcessId)
                .HasForeignKey<AssessmentData>(p => p.ProcessId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(p => p.CarisProjectDetails)
                .WithOne()
                .HasPrincipalKey<WorkflowInstance>(p => p.ProcessId)
                .HasForeignKey<CarisProjectDetails>(p => p.ProcessId);

            modelBuilder.Entity<WorkflowInstance>().Property(p => p.ActivityChangedAt).HasColumnType("date");
            modelBuilder.Entity<WorkflowInstance>().HasIndex(c => c.ProcessId).IsUnique();
            modelBuilder.Entity<WorkflowInstance>().HasIndex(c => new { c.ProcessId, c.PrimarySdocId }).IsUnique();
            modelBuilder.Entity<LinkedDocument>().HasIndex(c => new { c.ProcessId, c.LinkedSdocId, c.LinkType }).IsUnique();
            modelBuilder.Entity<PrimaryDocumentStatus>().Ignore(l => l.ContentServiceUri);
            modelBuilder.Entity<LinkedDocument>().Ignore(l => l.ContentServiceUri);
            modelBuilder.Entity<DatabaseDocumentStatus>().Ignore(l => l.ContentServiceUri);
            modelBuilder.Entity<OpenAssessmentsQueue>().HasIndex(o => o.PrimarySdocId).IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
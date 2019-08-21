using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF.Models;

namespace WorkflowDatabase.EF
{
    public class WorkflowDbContext : DbContext
    {
        public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
            : base(options)
        {
        }

        public DbSet<Models.AssessmentData> AssessmentData { get; set; }
        public DbSet<Models.Comments> Comment { get; set; }
        public DbSet<Models.DbAssessmentReviewData> DbAssessmentReviewData { get; set; }
        public DbSet<Models.WorkflowInstance> WorkflowInstance { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkflowInstance>().HasKey(x => x.WorkflowInstanceId);

            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.Comment);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(x => x.DbAssessmentReviewData);

            modelBuilder.Entity<WorkflowInstance>()
                .HasOne(p => p.AssessmentData)
                .WithOne()
                .HasPrincipalKey<WorkflowInstance>(p=>p.ProcessId)
                .HasForeignKey<AssessmentData>(p => p.ProcessId);

            modelBuilder.Entity<Comments>().HasKey(x => x.CommentId);
            modelBuilder.Entity<AssessmentData>().HasKey(x => x.AssessmentDataId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
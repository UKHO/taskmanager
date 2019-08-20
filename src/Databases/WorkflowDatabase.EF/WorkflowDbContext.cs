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
        public DbSet<Models.Comment> Comment { get; set; }
        public DbSet<Models.DbAssessmentReviewData> DbAssessmentReviewData { get; set; }
        public DbSet<Models.WorkflowInstance> WorkflowInstance { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorkflowInstance>()
                .HasMany(x => x.Comment);

            base.OnModelCreating(modelBuilder);
        }
    }
}
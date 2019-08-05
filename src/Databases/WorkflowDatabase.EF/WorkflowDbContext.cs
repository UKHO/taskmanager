using Microsoft.EntityFrameworkCore;

namespace WorkflowDatabase.EF
{
    public class WorkflowDbContext : DbContext
    {
        public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
            : base(options)
        {
        }

        public DbSet<Models.AssessmentData> AssessmentData { get; set; }
        public DbSet<Models.Task> Tasks { get; set; }
    }
}

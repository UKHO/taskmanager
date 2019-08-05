using Microsoft.EntityFrameworkCore;

namespace WorkflowDatabase.EF
{
    public class TasksDbContext : DbContext
    {
        public TasksDbContext(DbContextOptions<TasksDbContext> options)
            : base(options)
        {
        }

        public DbSet<Models.Task> Tasks { get; set; }
    }
}

using Microsoft.EntityFrameworkCore;

namespace Database.SQL.EF
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

using Microsoft.EntityFrameworkCore;

namespace HpdDatabase.EF.Models
{
    public class HpdDbContext : DbContext
    {
        public HpdDbContext(DbContextOptions<HpdDbContext> options)
            : base(options)
        {
        }

        public DbSet<CarisProject> CarisProjectData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CarisProject>().HasKey(x => x.ProjectId);
        }
    }
}

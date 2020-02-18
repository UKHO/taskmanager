using Microsoft.EntityFrameworkCore;

namespace HpdDatabase.EF.Models
{
    public class HpdDbContext : DbContext
    {
        public HpdDbContext(DbContextOptions<HpdDbContext> options)
            : base(options)
        {
        }

        public DbSet<CarisProduct> CarisProducts { get; set; }
        public DbSet<CarisProject> CarisProjectData { get; set; }
        public DbSet<CarisProjectType> CarisProjectTypes { get; set; }
        public DbSet<CarisProjectStatus> CarisProjectStatuses { get; set; }
        public DbSet<CarisProjectPriority> CarisProjectPriorities { get; set; }
        public DbSet<CarisWorkspace> CarisWorkspaces { get; set; }
        public DbSet<CarisUser> CarisUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CarisProject>().HasKey(x => x.ProjectId);
            modelBuilder.Entity<CarisProjectType>().HasKey(x => x.ProjectTypeId);
            modelBuilder.Entity<CarisProjectStatus>().HasKey(x => x.ProjectStatusId);
            modelBuilder.Entity<CarisProjectPriority>().HasKey(x => x.ProjectPriorityId);
            modelBuilder.Entity<CarisProduct>().HasKey(x => x.ProductName);
            modelBuilder.Entity<CarisWorkspace>().HasKey(x => x.Name);
            modelBuilder.Entity<CarisUser>().HasKey(x => x.UserId);
        }
    }
}
    
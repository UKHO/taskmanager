using Microsoft.EntityFrameworkCore;

namespace DataServices.Models
{
    public class SdraDbContext : DbContext
    {
        public SdraDbContext(DbContextOptions<SdraDbContext> options)
            : base(options)
        {
        }

        public DbSet<DocumentAssessmentData> AssessmentData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DocumentAssessmentData>().HasKey(x => x.SdocId);
        }
    }
}

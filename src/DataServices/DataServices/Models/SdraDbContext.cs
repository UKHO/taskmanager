using Microsoft.EntityFrameworkCore;

namespace DataServices.Models
{
    public class SdraDbContext : DbContext
    {
        public SdraDbContext(DbContextOptions<SdraDbContext> options)
            : base(options)
        {
        }
    }
}

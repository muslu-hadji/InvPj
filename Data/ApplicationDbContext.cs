using Microsoft.EntityFrameworkCore;
using InvPj.Models; // Убедитесь, что этот namespace правильный

namespace InvPj.Data // Это ваш корневой namespace
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Investor> Investors { get; set; }
        
        // Добавьте DbSet для InvestmentItem и Investment
        public DbSet<InvestmentItem> Investments { get; set; }
        //public DbSet<Investment> Investments { get; set; } \
        
    }
}

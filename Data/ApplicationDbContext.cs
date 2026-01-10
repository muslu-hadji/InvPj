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

        // Добавьте DbSet для вашей модели InvestmentItem
        public DbSet<InvestmentItem> InvestmentItems { get; set; }
    }
}

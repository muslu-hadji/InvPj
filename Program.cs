using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

// --- МОДЕЛИ ---
public class Investor {
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal InvestedAmount { get; set; }
    public decimal TotalEarned { get; set; } 
}

public class Deal {
    public int Id { get; set; }
    public string Title { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal MarkupAmount { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal RemainingDebt { get; set; }
    public virtual List<DealContribution> Contributions { get; set; } = new();
}

public class DealContribution {
    public int Id { get; set; }
    public int DealId { get; set; }
    public int InvestorId { get; set; }
    public decimal SharePercentage { get; set; }
    public decimal AmountInvested { get; set; }
    public virtual Investor Investor { get; set; }
}

// --- КОНТЕКСТ (С НОВОЙ БАЗОЙ inv_db) ---
public class AppDbContext : DbContext {
    public DbSet<Investor> Investors { get; set; }
    public DbSet<Deal> Deals { get; set; }
    public DbSet<DealContribution> Contributions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseNpgsql("Host=localhost;Database=inv_db;Username=postgres;Password=my_password");

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<DealContribution>().Property(p => p.SharePercentage).HasPrecision(18, 10);
    }
}

// --- ПРОГРАММА ---
class Program {
    static void Main() {
        using var db = new AppDbContext();
        
        // Удаляем старое и создаем чистое
        db.Database.EnsureDeleted(); 
        db.Database.EnsureCreated();

        // 1. Создаем инвесторов
        var invA = new Investor { Name = "Инвестор А", AvailableBalance = 70000 };
        var invB = new Investor { Name = "Инвестор Б", AvailableBalance = 30000 };
        db.Investors.AddRange(invA, invB);
        db.SaveChanges();

        // 2. Сделка на 10 000 + 2 000 наценка
        decimal principal = 10000;
        decimal markup = 2000;
        var deal = new Deal { 
            Title = "Рассрочка на товар", 
            PrincipalAmount = principal, 
            MarkupAmount = markup,
            TotalDebt = principal + markup,
            RemainingDebt = principal + markup
        };
        db.Deals.Add(deal);
        db.SaveChanges();

        // Распределяем вложения (70/30)
        var totalPool = db.Investors.Sum(i => i.AvailableBalance);
        foreach (var inv in db.Investors.ToList()) {
            decimal share = inv.AvailableBalance / totalPool;
            decimal contribution = principal * share;
            db.Contributions.Add(new DealContribution { DealId = deal.Id, InvestorId = inv.Id, SharePercentage = share, AmountInvested = contribution });
            inv.AvailableBalance -= contribution;
            inv.InvestedAmount += contribution;
        }
        db.SaveChanges();

        // 3. ПЛАТЕЖ 6 000 руб.
        decimal payment = 6000;
        decimal platformFeePercent = 0.20m;
        
        decimal principalRatio = deal.PrincipalAmount / deal.TotalDebt;
        decimal principalPaid = payment * principalRatio;
        decimal profitPaid = payment - principalPaid;
        decimal platformCut = profitPaid * platformFeePercent;
        decimal netProfit = profitPaid - platformCut;

        foreach (var cont in db.Contributions.Include(c => c.Investor).ToList()) {
            var inv = cont.Investor;
            inv.AvailableBalance += (principalPaid * cont.SharePercentage);
            inv.InvestedAmount -= (principalPaid * cont.SharePercentage);
            inv.TotalEarned += (netProfit * cont.SharePercentage);
            inv.AvailableBalance += (netProfit * cont.SharePercentage);
        }

        deal.RemainingDebt -= payment;
        db.SaveChanges();

        Console.WriteLine("✅ ВСЁ СРАБОТАЛО!");
        Console.WriteLine($"💰 Принят платеж: {payment} руб.");
        Console.WriteLine($"🏢 Доход системы: {platformCut} руб.");
        Console.WriteLine("\n📊 СОСТОЯНИЕ СЧЕТОВ:");
        foreach (var inv in db.Investors) {
            Console.WriteLine($"{inv.Name}: Баланс {inv.AvailableBalance:N2} | Заработал {inv.TotalEarned:N2}");
        }
    }
}

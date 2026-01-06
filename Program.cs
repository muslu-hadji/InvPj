using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class Investor {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal AvailableBalance { get; set; }
    public decimal InvestedAmount { get; set; }
    public decimal TotalEarned { get; set; }
}

public class Deal {
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public decimal PrincipalAmount { get; set; }
    public decimal MarkupAmount { get; set; }
    public decimal RemainingDebt { get; set; }
}

public class DealContribution {
    public int Id { get; set; }
    public int DealId { get; set; }
    public int InvestorId { get; set; }
    public decimal SharePercentage { get; set; }
    public decimal AmountInvested { get; set; }
    public virtual Investor? Investor { get; set; }
}

public class AppDbContext : DbContext {
    public DbSet<Investor> Investors => Set<Investor>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<DealContribution> Contributions => Set<DealContribution>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseNpgsql("Host=localhost;Database=inv_db;Username=postgres;Password=my_password");
}

class Program {
    static async Task Main() {
        using (var db = new AppDbContext()) db.Database.EnsureCreated();
        var botClient = new TelegramBotClient("8551799916:AAH7PdzEJhpzAcFV6eEgqCjTV_7rb4rBYh4");
        using var cts = new CancellationTokenSource();

        botClient.StartReceiving(
            async (bot, update, ct) => {
                if (update.Message is not { Text: { } messageText } message) return;
                var chatId = message.Chat.Id;
                var args = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                using var db = new AppDbContext();

                try {
                    string cmd = args[0].ToLower();
                    if (cmd == "/start") {
                        await bot.SendMessage(chatId, "💎 ГЛАВНОЕ МЕНЮ:\n/status — Балансы\n/deals — Список сделок\n/pay [ID] [Сумма] — Внести оплату\n/add_investor [Имя] [Сумма]\n/new_deal [Название] [Сумма] [Наценка]", cancellationToken: ct);
                    }
                    else if (cmd == "/status") {
                        var invs = db.Investors.ToList();
                        await bot.SendMessage(chatId, "📊 БАЛАНСЫ:\n" + string.Join("\n", invs.Select(i => $"👤 {i.Name}: {i.AvailableBalance:N0} (Прибыль: {i.TotalEarned:N0})")), cancellationToken: ct);
                    }
                    else if (cmd == "/deals") {
                        var deals = db.Deals.Where(d => d.RemainingDebt > 0).ToList();
                        await bot.SendMessage(chatId, "📝 СДЕЛКИ:\n" + (deals.Count == 0 ? "Нет активных" : string.Join("\n", deals.Select(d => $"ID: {d.Id} | {d.Title} | Долг: {d.RemainingDebt:N0}"))), cancellationToken: ct);
                    }
                    else if (cmd == "/pay" && args.Length == 3) {
                        int dId = int.Parse(args[1]);
                        decimal sum = decimal.Parse(args[2]);
                        var deal = db.Deals.Find(dId);
                        if (deal == null) { await bot.SendMessage(chatId, "❌ Сделка не найдена", cancellationToken: ct); return; }

                        decimal total = deal.PrincipalAmount + deal.MarkupAmount;
                        decimal pRatio = deal.PrincipalAmount / total;
                        decimal pPart = sum * pRatio;
                        decimal profit = sum - pPart;

                        var conts = db.Contributions.Include(c => c.Investor).Where(c => c.DealId == dId).ToList();
                        foreach (var c in conts) {
                            decimal invProfit = profit * 0.8m * c.SharePercentage;
                            c.Investor!.AvailableBalance += (pPart * c.SharePercentage) + invProfit;
                            c.Investor.TotalEarned += invProfit;
                        }
                        deal.RemainingDebt -= sum;
                        await db.SaveChangesAsync();
                        await bot.SendMessage(chatId, $"💰 Платеж {sum:N0} принят!", cancellationToken: ct);
                    }
                } catch { await bot.SendMessage(chatId, "⚠️ Ошибка! Проверь формат команды.", cancellationToken: ct); }
            },
            (bot, ex, ct) => Task.CompletedTask,
            new ReceiverOptions { AllowedUpdates = [] },
            cts.Token
        );
        Console.WriteLine("🤖 Бот запущен!");
        await Task.Delay(-1);
    }
}

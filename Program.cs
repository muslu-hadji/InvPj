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
                        await bot.SendMessage(chatId, "💰 Финтех-система готова!\n\nКоманды:\n/add_investor Имя Сумма\n/status\n/new_deal Название Сумма Наценка", cancellationToken: ct);
                    }
                    else if (cmd == "/add_investor" && args.Length == 3) {
                        var name = args[1];
                        if (decimal.TryParse(args[2], out decimal balance)) {
                            db.Investors.Add(new Investor { Name = name, AvailableBalance = balance });
                            await db.SaveChangesAsync();
                            await bot.SendMessage(chatId, $"✅ Инвестор {name} успешно добавлен!", cancellationToken: ct);
                        }
                    }
                    else if (cmd == "/status") {
                        var invs = db.Investors.ToList();
                        if (invs.Count == 0) {
                            await bot.SendMessage(chatId, "В базе пока нет инвесторов.", cancellationToken: ct);
                        } else {
                            var report = "📊 ТЕКУЩИЕ БАЛАНСЫ:\n" + string.Join("\n", invs.Select(i => $"👤 {i.Name}: {i.AvailableBalance:N0} руб."));
                            await bot.SendMessage(chatId, report, cancellationToken: ct);
                        }
                    }
                    else if (cmd == "/new_deal" && args.Length == 4) {
                        string title = args[1];
                        decimal amount = decimal.Parse(args[2]);
                        decimal markup = decimal.Parse(args[3]);
                        
                        var totalAvailable = db.Investors.Sum(i => i.AvailableBalance);
                        if (totalAvailable < amount) {
                            await bot.SendMessage(chatId, $"❌ Недостаточно средств! В пуле всего {totalAvailable:N0} руб., а нужно {amount:N0}.", cancellationToken: ct);
                            return;
                        }

                        var deal = new Deal { Title = title, PrincipalAmount = amount, MarkupAmount = markup, RemainingDebt = amount + markup };
                        db.Deals.Add(deal);
                        await db.SaveChangesAsync();

                        foreach (var inv in db.Investors.Where(i => i.AvailableBalance > 0).ToList()) {
                            decimal share = inv.AvailableBalance / totalAvailable;
                            decimal contribution = amount * share;
                            db.Contributions.Add(new DealContribution { DealId = deal.Id, InvestorId = inv.Id, SharePercentage = share, AmountInvested = contribution });
                            inv.AvailableBalance -= contribution;
                            inv.InvestedAmount += contribution;
                        }
                        await db.SaveChangesAsync();
                        await bot.SendMessage(chatId, $"🚀 Сделка '{title}' на {amount:N0} руб. создана!\nСумма распределена между всеми инвесторами.", cancellationToken: ct);
                    }
                } catch (Exception ex) {
                    await bot.SendMessage(chatId, "⚠️ Ошибка обработки. Проверь, что вводишь числа без пробелов внутри (например, 15000 вместо 15 000).", cancellationToken: ct);
                }
            },
            (bot, ex, ct) => Task.CompletedTask,
            new ReceiverOptions { AllowedUpdates = [] },
            cts.Token
        );
        Console.WriteLine("🤖 Бот запущен!");
        await Task.Delay(-1);
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public class Investor {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal AvailableBalance { get; set; }
    public decimal TotalEarned { get; set; }
}

public class AppDbContext : DbContext {
    public DbSet<Investor> Investors => Set<Investor>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseNpgsql("Host=localhost;Database=inv_db;Username=postgres;Password=my_password");
}

class Program {
    static async Task Main() {
        using (var db = new AppDbContext()) db.Database.EnsureCreated();
        var botClient = new TelegramBotClient("8551799916:AAH7PdzEJhpzAcFV6eEgqCjTV_7rb4rBYh4");
        
        botClient.StartReceiving(
            async (bot, update, ct) => {
                if (update.Message is not { Text: { } messageText } message) return;
                var chatId = message.Chat.Id;
                using var db = new AppDbContext();

                if (messageText.ToLower() == "/start") {
                    var csName = Environment.GetEnvironmentVariable("CODESPACE_NAME");
                    var webAppUrl = $"https://{csName}-5000.app.github.dev/";
                    
                    var keyboard = new ReplyKeyboardMarkup(new[] {
                        new KeyboardButton("📱 Открыть приложение") { WebApp = new WebAppInfo { Url = webAppUrl } }
                    }) { ResizeKeyboard = true };

                    await bot.SendMessage(chatId, "Добро пожаловать в мобильный интерфейс!", replyMarkup: keyboard);
                }
            },
            (bot, ex, ct) => Task.CompletedTask,
            new ReceiverOptions { AllowedUpdates = [] }
        );
        Console.WriteLine("🤖 Бот запущен!");
        await Task.Delay(-1);
    }
}

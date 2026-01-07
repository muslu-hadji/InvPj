using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

var builder = WebApplication.CreateBuilder();

// РАЗРЕШАЕМ ВСЁ (CORS)
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors(); // Применяем разрешение
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/balance", async () => {
    try {
        using var db = new AppDbContext();
        var total = await db.Investors.SumAsync(i => i.AvailableBalance);
        return Results.Ok(new { totalBalance = total });
    } catch {
        return Results.Ok(new { totalBalance = 0 });
    }
});

var botClient = new TelegramBotClient("8551799916:AAH7PdzEJhpzAcFV6eEgqCjTV_7rb4rBYh4");

_ = Task.Run(() => botClient.StartReceiving(
    async (bot, update, ct) => {
        if (update.Message?.Text == "/start") {
            var host = Environment.GetEnvironmentVariable("CODESPACE_NAME");
            var url = $"https://{host}-5000.app.github.dev/";
            var kb = new ReplyKeyboardMarkup(new[] {
                new KeyboardButton("📱 Открыть приложение") { WebApp = new WebAppInfo { Url = url } }
            }) { ResizeKeyboard = true };
            await bot.SendTextMessageAsync(update.Message.Chat.Id, "Обновлено! Открывай:", replyMarkup: kb);
        }
    },
    (bot, ex, ct) => Task.CompletedTask
));

await app.RunAsync("http://0.0.0.0:5000");

public class Investor {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal AvailableBalance { get; set; }
}

public class AppDbContext : DbContext {
    public DbSet<Investor> Investors => Set<Investor>();
    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseNpgsql("Host=localhost;Database=inv_db;Username=postgres;Password=my_password");
}

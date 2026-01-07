using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Добавляем CORS
builder.Services.AddCors(options => 
    options.AddDefaultPolicy(p => 
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod()));

// Добавляем DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql("Host=127.0.0.1;Database=invest_db;Username=postgres"));

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// Endpoint для получения баланса
app.MapGet("/api/balance", async (AppDbContext db) => {
    await db.Database.EnsureCreatedAsync();
    var total = await db.Investors.SumAsync(i => (decimal?)i.AvailableBalance) ?? 0;
    return Results.Ok(new { totalBalance = total });
});

// Endpoint для добавления инвестора
app.MapPost("/api/investors", async (Investor inv, AppDbContext db) => {
    db.Investors.Add(inv);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Investor added", investorId = inv.Id });
});

// Endpoint для получения всех инвесторов
app.MapGet("/api/investors", async (AppDbContext db) => {
    var investors = await db.Investors.ToListAsync();
    return Results.Ok(investors);
});

await app.RunAsync("http://0.0.0.0:5000");

public class Investor {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal AvailableBalance { get; set; }
}

public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Investor> Investors => Set<Investor>();
}

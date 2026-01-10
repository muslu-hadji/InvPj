using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InvPj.Data;
using InvPj.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => 
    options.AddDefaultPolicy(p => 
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod()));

//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseNpgsql("Host=127.0.0.1;Database=invest_db;Username=postgres;Password=my_app_password"));

Console.WriteLine($"DEBUG: Connection String is: '{builder.Configuration.GetConnectionString("DefaultConnection")}'");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection"); // Или ваша строка подключения напрямую
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<InvestmentService>(); 
builder.Services.AddRazorPages(); 


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    Console.WriteLine($"DEBUG: Connection String is: '{builder.Configuration.GetConnectionString("DefaultConnection")}'");
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/balance", async (ApplicationDbContext db) => 
{
    var total = await db.Investors.SumAsync(i => (decimal?)i.AvailableBalance) ?? 0;
    return Results.Ok(new { totalBalance = total });
});

app.MapPost("/api/investors", async (Investor inv, ApplicationDbContext db) => 
{
    db.Investors.Add(inv);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Investor added", investorId = inv.Id });
});

app.MapGet("/api/investors", async (ApplicationDbContext db) => 
{
    var investors = await db.Investors.ToListAsync();
    return Results.Ok(investors);
});

app.MapRazorPages(); 

app.Run();


await app.RunAsync("http://0.0.0.0:5000");

public class Investor 
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal AvailableBalance { get; set; }
}

//public class AppDbContext : DbContext 
//{
//    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
//    public DbSet<Investor> Investors => Set<Investor>();
//}
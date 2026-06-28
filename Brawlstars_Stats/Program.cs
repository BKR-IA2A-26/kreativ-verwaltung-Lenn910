using Brawlstars_Stats.Models;
using Microsoft.EntityFrameworkCore;
    

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Den Connection String aus der appsettings.json laden
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Den DbContext mit dem MySQL Provider registrieren
builder.Services.AddDbContext<BrawlDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Brawl Stars API Service & HttpClient registrieren
builder.Services.AddHttpClient<Brawlstars_Stats.Services.BrawlStarsApiService>();

var app = builder.Build();

// Führe sicheres DB Update beim Start aus, um die neue Trophäen-Spalte hinzuzufügen
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BrawlDbContext>();
    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE werte ADD COLUMN pokal_veraenderung INT NULL DEFAULT 0;");
    }
    catch
    {
        // Spalte existiert wahrscheinlich bereits, Fehler ignorieren
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

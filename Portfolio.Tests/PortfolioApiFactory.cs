using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using portfolio_backend.Data;

namespace Portfolio.Tests;

// WebApplicationFactory<Program> start jouw echte API in het geheugen.
// Wij passen alleen aan wat voor tests anders moet: de database en de testgebruikers.
public class PortfolioApiFactory : WebApplicationFactory<Program>
{
    // Vaste testgebruikers. Alleen voor de test-database, dus niet geheim.
    public const string EditorEmail = "editor@test.local";
    public const string AdminEmail = "admin@test.local";
    public const string Wachtwoord = "Test@2005!";

    // SQLite in het geheugen bestaat zolang deze verbinding open is.
    // Eén verbinding per factory = één lege test-database per testklasse.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public PortfolioApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Andere omgeving dan Development: de seeder in Program.cs en de user secrets draaien dan niet.
        builder.UseEnvironment("Testing");

        // Seed-gegevens voor de test-database (zelfde sleutels als in je user secrets).
        builder.UseSetting("Seed:EditorEmail", EditorEmail);
        builder.UseSetting("Seed:EditorPassword", Wachtwoord);
        builder.UseSetting("Seed:AdminEmail", AdminEmail);
        builder.UseSetting("Seed:AdminPassword", Wachtwoord);

        // ConfigureTestServices draait NA Program.cs, dus we kunnen registraties vervangen.
        builder.ConfigureTestServices(services =>
        {
            // De SQL Server-registratie uit Program.cs weghalen.
            services.RemoveAll<DbContextOptions<PortfolioDb>>();
            services.RemoveAll<IDbContextOptionsConfiguration<PortfolioDb>>();

            // En vervangen door SQLite in het geheugen: geïsoleerd van je echte database.
            services.AddDbContext<PortfolioDb>(options => options.UseSqlite(_connection));
        });
    }

    // Draait één keer als de test-API opstart.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Eigen scope, net als in de seeder: er is nog geen request.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioDb>();

        // Maakt alle tabellen (ook Identity) in de lege SQLite-database.
        // EnsureCreated en geen migrations, want je migrations zijn voor SQL Server geschreven.
        db.Database.EnsureCreated();

        // Je ECHTE seeder hergebruiken: rollen + Editor en Admin aanmaken.
        var config = host.Services.GetRequiredService<IConfiguration>();
        DbSeeder.SeedAsync(host.Services, config).GetAwaiter().GetResult();

        return host;
    }

    // Opruimen: verbinding sluiten = test-database weg.
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
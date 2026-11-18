using System.Net.Http.Json;      // PostAsJsonAsync / ReadFromJsonAsync: object <-> JSON
using portfolio_backend.Auth;    // Roles
using portfolio_backend.Dtos;

namespace Portfolio.Tests;

// Hulpmethodes voor het ARRANGE-deel van de tests: inloggen en testdata klaarzetten.
// Zo blijven de tests zelf kort en lees je meteen wat er getest wordt.
public static class TestHelpers
{
    // ===== INLOGGEN =====

    // Logt in via de ECHTE loginroute van de API.
    // De HttpClient van WebApplicationFactory bewaart de cookie en stuurt hem
    // bij volgende requests automatisch mee (net als een browser).
    public static async Task LoginAsync(HttpClient client, string email, string wachtwoord)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = wachtwoord });

        // Lukt de login niet, dan stopt de test hier met een duidelijke fout
        // (dan zit het probleem in de Arrange, niet in wat je test).
        response.EnsureSuccessStatusCode();
    }

    // Editor en Admin zijn door de seeder in de test-database gezet (zie PortfolioApiFactory).
    public static Task LoginAlsEditorAsync(HttpClient client) =>
        LoginAsync(client, PortfolioApiFactory.EditorEmail, PortfolioApiFactory.Wachtwoord);

    public static Task LoginAlsAdminAsync(HttpClient client) =>
        LoginAsync(client, PortfolioApiFactory.AdminEmail, PortfolioApiFactory.Wachtwoord);

    // Een Gast bestaat nog niet: registreer er een via de echte registratieroute
    // (die geeft altijd de rol Gast) en log daarna in.
    // Guid = uniek e-mailadres, zodat tests onafhankelijk zijn van elkaar en van de volgorde.
    public static async Task LoginAlsGastAsync(HttpClient client)
    {
        var email = $"gast-{Guid.NewGuid():N}@test.local";

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { Email = email, Password = PortfolioApiFactory.Wachtwoord });
        response.EnsureSuccessStatusCode();

        await LoginAsync(client, email, PortfolioApiFactory.Wachtwoord);
    }

    // Inloggen op basis van een rolnaam. Hiermee kan één [Theory] dezelfde test
    // draaien als Editor ÉN als Admin: [InlineData(Roles.Editor)] en [InlineData(Roles.Admin)].
    public static Task LoginAlsRolAsync(HttpClient client, string rol) => rol switch
    {
        Roles.Editor => LoginAlsEditorAsync(client),
        Roles.Admin => LoginAlsAdminAsync(client),
        Roles.Gast => LoginAlsGastAsync(client),
        _ => throw new ArgumentException($"Onbekende rol: {rol}")
    };

    // ===== TESTDATA =====

    // Unieke titel, zodat je in een gedeelde test-database precies jouw item terugvindt.
    public static string UniekeTitel(string voorvoegsel) => $"{voorvoegsel} {Guid.NewGuid():N}";

    // Een geldig project. Titel en link kun je aanpassen om ongeldige invoer te testen.
    public static ProjectRequest NieuwProject(
        string titel = "Testproject",
        string gitHubUrl = "https://github.com/erimprojects") => new()
    {
        Titel = titel,
        Beschrijving = "Beschrijving van het testproject",
        Categorie = "Backend",
        GitHubUrl = gitHubUrl,
        Datum = new DateTime(2026, 11, 18)
    };

    // Een geldige blogpost. Titel en inhoud kun je aanpassen om ongeldige invoer te testen.
    public static BlogpostRequest NieuwBlogpost(
        string titel = "Testblog",
        string inhoud = "Inhoud van de testblog") => new()
    {
        Titel = titel,
        Inhoud = inhoud,
        Publicatiedatum = new DateTime(2026, 11, 18)
    };

    // Zet een bestaand project in de test-database (als Editor), voor tests die
    // iets met een BESTAAND project doen (GET op id, PUT, DELETE).
    public static async Task<ProjectResponse> MaakProjectAanAsync(PortfolioApiFactory factory)
    {
        var client = factory.CreateClient();
        await LoginAlsEditorAsync(client);

        var response = await client.PostAsJsonAsync("/api/projects", NieuwProject(UniekeTitel("Bestaand project")));
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ProjectResponse>())!;
    }

    // Zelfde, maar voor een blogpost.
    public static async Task<BlogpostResponse> MaakBlogpostAanAsync(PortfolioApiFactory factory)
    {
        var client = factory.CreateClient();
        await LoginAlsEditorAsync(client);

        var response = await client.PostAsJsonAsync("/api/blogposts", NieuwBlogpost(UniekeTitel("Bestaande blog")));
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<BlogpostResponse>())!;
    }
}

using System.Net;                           // HttpStatusCode
using System.Net.Http.Json;                 // PostAsJsonAsync, ReadFromJsonAsync
using Microsoft.AspNetCore.Identity;        // UserManager
using Microsoft.AspNetCore.Mvc.Testing;     // WebApplicationFactoryClientOptions
using Microsoft.Extensions.DependencyInjection;
using portfolio_backend.Auth;               // Roles
using portfolio_backend.Dtos;
using portfolio_backend.Models;             // User

namespace Portfolio.Tests;

// Tests voor de eigen registratie- en loginroute, met de ECHTE Identity-configuratie
// (UserManager, SignInManager, wachtwoordregels, cookie) uit Program.cs.
public class AuthTests : IClassFixture<PortfolioApiFactory>
{
    private const string RegisterUrl = "/api/auth/register";
    private const string LoginUrl = "/api/auth/login";

    // Naam van de cookie die Identity bij een geslaagde login zet.
    private const string AuthCookieNaam = ".AspNetCore.Identity.Application";

    private readonly PortfolioApiFactory _factory;

    public AuthTests(PortfolioApiFactory factory)
    {
        _factory = factory;
    }

    // Vorm van het antwoord van /register: { "email": "...", "rol": "Gast" }.
    private record RegisterAntwoord(string Email, string Rol);

    // Haalt de rollen van een gebruiker rechtstreeks uit de test-database,
    // zodat we niet alleen het antwoord controleren maar ook wat er echt is opgeslagen.
    private async Task<(User? user, IList<string> rollen)> HaalGebruikerOpAsync(string email)
    {
        // Eigen scope, omdat UserManager scoped is (zoals in de seeder).
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = await userManager.FindByEmailAsync(email);
        var rollen = user is null ? new List<string>() : await userManager.GetRolesAsync(user);
        return (user, rollen);
    }

    // =====================================================================
    // POST /api/auth/register
    // =====================================================================

    [Fact]
    public async Task Register_GeldigeGegevens_Geeft200EnRolGast()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = $"nieuw-{Guid.NewGuid():N}@test.local";

        // Act
        var response = await client.PostAsJsonAsync(RegisterUrl,
            new RegisterRequest { Email = email, Password = PortfolioApiFactory.Wachtwoord });

        // Assert 1: 200 en het antwoord noemt de rol Gast.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var antwoord = await response.Content.ReadFromJsonAsync<RegisterAntwoord>();
        Assert.Equal(email, antwoord!.Email);
        Assert.Equal(Roles.Gast, antwoord.Rol);

        // Assert 2 (effect): de gebruiker staat in de database met ALLEEN de rol Gast,
        // en het wachtwoord is gehasht (niet als leesbare tekst opgeslagen).
        var (user, rollen) = await HaalGebruikerOpAsync(email);
        Assert.NotNull(user);
        Assert.Equal(Roles.Gast, Assert.Single(rollen));
        Assert.NotEqual(PortfolioApiFactory.Wachtwoord, user.PasswordHash);
    }

    [Fact]
    public async Task Register_MetRolAdminInBody_KrijgtTochAlleenGast()
    {
        // Arrange: een kwaadwillende client stuurt zelf "rol": "Admin" mee.
        var client = _factory.CreateClient();
        var email = $"hacker-{Guid.NewGuid():N}@test.local";
        var body = new { email, password = PortfolioApiFactory.Wachtwoord, rol = Roles.Admin };

        // Act
        var response = await client.PostAsJsonAsync(RegisterUrl, body);

        // Assert: het extra veld wordt genegeerd (RegisterRequest heeft geen rol-veld).
        // Publieke registratie kent dus NOOIT Editor of Admin toe.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (_, rollen) = await HaalGebruikerOpAsync(email);
        Assert.Equal(Roles.Gast, Assert.Single(rollen));
    }

    [Fact]
    public async Task Register_BestaandEmail_Geeft400()
    {
        // Arrange: de Editor bestaat al (aangemaakt door de seeder).
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(RegisterUrl,
            new RegisterRequest { Email = PortfolioApiFactory.EditorEmail, Password = PortfolioApiFactory.Wachtwoord });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ZwakWachtwoord_Geeft400EnMaaktGeenGebruiker()
    {
        // Arrange: "abc" voldoet niet aan de wachtwoordregels van Identity.
        var client = _factory.CreateClient();
        var email = $"zwak-{Guid.NewGuid():N}@test.local";

        // Act
        var response = await client.PostAsJsonAsync(RegisterUrl,
            new RegisterRequest { Email = email, Password = "abc" });

        // Assert: 400 én er is geen gebruiker aangemaakt.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var (user, _) = await HaalGebruikerOpAsync(email);
        Assert.Null(user);
    }

    [Fact]
    public async Task Register_OngeldigEmail_Geeft400()
    {
        // Arrange: [EmailAddress] in RegisterRequest keurt dit af.
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(RegisterUrl,
            new RegisterRequest { Email = "geen-email", Password = PortfolioApiFactory.Wachtwoord });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =====================================================================
    // POST /api/auth/login
    // =====================================================================

    [Fact]
    public async Task Login_JuisteGegevens_Geeft200EnZetAuthCookie()
    {
        // Arrange: HandleCookies = false, zodat de client de cookie niet "opeet"
        // en we de Set-Cookie-header zelf kunnen bekijken.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        // Act
        var response = await client.PostAsJsonAsync(LoginUrl,
            new LoginRequest { Email = PortfolioApiFactory.EditorEmail, Password = PortfolioApiFactory.Wachtwoord });

        // Assert: 200 én de response bevat de authenticatiecookie.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, c => c.StartsWith(AuthCookieNaam));
    }

    [Fact]
    public async Task Login_FoutWachtwoord_Geeft401EnGeenCookie()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        // Act
        var response = await client.PostAsJsonAsync(LoginUrl,
            new LoginRequest { Email = PortfolioApiFactory.EditorEmail, Password = "FoutWachtwoord1!" });

        // Assert: 401 én er wordt geen cookie gezet.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Login_OnbekendeGebruiker_Geeft401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(LoginUrl,
            new LoginRequest { Email = "bestaat-niet@test.local", Password = PortfolioApiFactory.Wachtwoord });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_LegeInvoer_Geeft400()
    {
        // Arrange: [Required] in LoginRequest keurt lege velden af.
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(LoginUrl,
            new LoginRequest { Email = "", Password = "" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

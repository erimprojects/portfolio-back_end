using System.Net;                // HttpStatusCode
using System.Net.Http.Json;      // PostAsJsonAsync, PutAsJsonAsync, ReadFromJsonAsync
using portfolio_backend.Auth;    // Roles (voor [InlineData])
using portfolio_backend.Dtos;

namespace Portfolio.Tests;

// Integratietests via HTTP voor alle 5 blogpostroutes.
// IClassFixture<PortfolioApiFactory>: xUnit maakt één test-API met één lege SQLite-database
// voor alle tests in deze klasse en geeft die via de constructor mee.
// Elke test maakt zijn eigen HttpClient (= eigen "browser"), dus begint niet ingelogd.
public class BlogpostsTests : IClassFixture<PortfolioApiFactory>
{
    private const string Url = "/api/blogposts";
    private readonly PortfolioApiFactory _factory;

    public BlogpostsTests(PortfolioApiFactory factory)
    {
        _factory = factory;
    }

    // =====================================================================
    // GET /api/blogposts en GET /api/blogposts/{id}  –  publiek (iedereen)
    // =====================================================================

    [Fact]
    public async Task GetAll_ZonderLogin_Geeft200EnBevatBestaandeBlogpost()
    {
        // Arrange: er staat een blogpost in de database, client is niet ingelogd.
        var bestaand = await TestHelpers.MaakBlogpostAanAsync(_factory);
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(Url);

        // Assert: statuscode én inhoud (de blogpost zit in de lijst).
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var blogposts = await response.Content.ReadFromJsonAsync<List<BlogpostResponse>>();
        Assert.NotNull(blogposts);
        Assert.Contains(blogposts, p => p.Id == bestaand.Id && p.Titel == bestaand.Titel);
    }

    [Fact]
    public async Task GetById_BestaandId_Geeft200EnJuisteGegevens()
    {
        // Arrange
        var bestaand = await TestHelpers.MaakBlogpostAanAsync(_factory);
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"{Url}/{bestaand.Id}");

        // Assert: BlogpostResponse is een record, dus Assert.Equal vergelijkt ALLE velden.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var gevonden = await response.Content.ReadFromJsonAsync<BlogpostResponse>();
        Assert.Equal(bestaand, gevonden);
    }

    [Fact]
    public async Task GetById_OnbekendId_Geeft404()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"{Url}/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =====================================================================
    // POST /api/blogposts  –  alleen Editor of Admin
    // =====================================================================

    // [Theory] + [InlineData]: dezelfde test draait twee keer, als Editor én als Admin.
    [Theory]
    [InlineData(Roles.Editor)]
    [InlineData(Roles.Admin)]
    public async Task Post_AlsEditorOfAdmin_Geeft201EnSlaatBlogpostOp(string rol)
    {
        // Arrange
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsRolAsync(client, rol);
        var nieuw = TestHelpers.NieuwBlogpost(TestHelpers.UniekeTitel("Nieuwe blog"));

        // Act
        var response = await client.PostAsJsonAsync(Url, nieuw);

        // Assert 1: 201 Created + Location-header naar de nieuwe blogpost.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        // Assert 2: de response bevat de verstuurde gegevens + een nieuw Id van de database.
        var aangemaakt = await response.Content.ReadFromJsonAsync<BlogpostResponse>();
        Assert.NotNull(aangemaakt);
        Assert.True(aangemaakt.Id > 0);
        Assert.Equal(nieuw.Titel, aangemaakt.Titel);
        Assert.Equal(nieuw.Inhoud, aangemaakt.Inhoud);

        // Assert 3 (effect): de blogpost is echt opgeslagen en via GET terug te vinden.
        var opgehaald = await client.GetFromJsonAsync<BlogpostResponse>($"{Url}/{aangemaakt.Id}");
        Assert.Equal(aangemaakt, opgehaald);
    }

    [Fact]
    public async Task Post_ZonderLogin_Geeft401EnSlaatNietsOp()
    {
        // Arrange: niet ingelogd.
        var client = _factory.CreateClient();
        var nieuw = TestHelpers.NieuwBlogpost(TestHelpers.UniekeTitel("Geweigerde blog"));

        // Act
        var response = await client.PostAsJsonAsync(Url, nieuw);

        // Assert: 401 én de blogpost staat NIET in de database.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var blogposts = await client.GetFromJsonAsync<List<BlogpostResponse>>(Url);
        Assert.DoesNotContain(blogposts!, p => p.Titel == nieuw.Titel);
    }

    [Fact]
    public async Task Post_AlsGast_Geeft403EnSlaatNietsOp()
    {
        // Arrange: ingelogd, maar alleen met de rol Gast.
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsGastAsync(client);
        var nieuw = TestHelpers.NieuwBlogpost(TestHelpers.UniekeTitel("Geweigerde blog"));

        // Act
        var response = await client.PostAsJsonAsync(Url, nieuw);

        // Assert: 403 én de blogpost staat NIET in de database.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var blogposts = await client.GetFromJsonAsync<List<BlogpostResponse>>(Url);
        Assert.DoesNotContain(blogposts!, p => p.Titel == nieuw.Titel);
    }

    // Ongeldige invoer: [Required] in BlogpostRequest geeft 400 nog vóór de service draait.
    [Theory]
    [InlineData("", "Inhoud")]            // lege titel
    [InlineData("   ", "Inhoud")]         // titel met alleen spaties
    [InlineData("Geldige titel", "")]     // lege inhoud
    public async Task Post_OngeldigeInvoer_Geeft400(string titel, string inhoud)
    {
        // Arrange
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsEditorAsync(client);
        var ongeldig = TestHelpers.NieuwBlogpost(titel, inhoud);

        // Act
        var response = await client.PostAsJsonAsync(Url, ongeldig);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =====================================================================
    // PUT /api/blogposts/{id}  –  alleen Editor of Admin
    // =====================================================================

    [Theory]
    [InlineData(Roles.Editor)]
    [InlineData(Roles.Admin)]
    public async Task Put_AlsEditorOfAdmin_Geeft204EnWijzigtBlogpost(string rol)
    {
        // Arrange
        var bestaand = await TestHelpers.MaakBlogpostAanAsync(_factory);
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsRolAsync(client, rol);
        var wijziging = TestHelpers.NieuwBlogpost(TestHelpers.UniekeTitel("Gewijzigde blog"));

        // Act
        var response = await client.PutAsJsonAsync($"{Url}/{bestaand.Id}", wijziging);

        // Assert: 204 én (effect) de nieuwe titel staat in de database.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var opgehaald = await client.GetFromJsonAsync<BlogpostResponse>($"{Url}/{bestaand.Id}");
        Assert.Equal(wijziging.Titel, opgehaald!.Titel);
    }

    [Fact]
    public async Task Put_ZonderLogin_Geeft401EnWijzigtNiets()
    {
        // Arrange
        var bestaand = await TestHelpers.MaakBlogpostAanAsync(_factory);
        var client = _factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync($"{Url}/{bestaand.Id}", TestHelpers.NieuwBlogpost("Mag niet"));

        // Assert: 401 én de oude titel staat er nog.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var opgehaald = await client.GetFromJsonAsync<BlogpostResponse>($"{Url}/{bestaand.Id}");
        Assert.Equal(bestaand.Titel, opgehaald!.Titel);
    }

    [Fact]
    public async Task Put_AlsGast_Geeft403EnWijzigtNiets()
    {
        // Arrange
        var bestaand = await TestHelpers.MaakBlogpostAanAsync(_factory);
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsGastAsync(client);

        // Act
        var response = await client.PutAsJsonAsync($"{Url}/{bestaand.Id}", TestHelpers.NieuwBlogpost("Mag niet"));

        // Assert: 403 én de oude titel staat er nog.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var opgehaald = await client.GetFromJsonAsync<BlogpostResponse>($"{Url}/{bestaand.Id}");
        Assert.Equal(bestaand.Titel, opgehaald!.Titel);
    }

    [Fact]
    public async Task Put_OnbekendId_Geeft404()
    {
        // Arrange
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsEditorAsync(client);

        // Act
        var response = await client.PutAsJsonAsync($"{Url}/999999", TestHelpers.NieuwBlogpost());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_OngeldigeInvoer_Geeft400EnWijzigtNiets()
    {
        // Arrange
        var bestaand = await TestHelpers.MaakBlogpostAanAsync(_factory);
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsEditorAsync(client);

        // Act: lege titel.
        var response = await client.PutAsJsonAsync($"{Url}/{bestaand.Id}", TestHelpers.NieuwBlogpost(""));

        // Assert: 400 én de oude titel staat er nog.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var opgehaald = await client.GetFromJsonAsync<BlogpostResponse>($"{Url}/{bestaand.Id}");
        Assert.Equal(bestaand.Titel, opgehaald!.Titel);
    }

    // =====================================================================
    // DELETE /api/blogposts/{id}  –  alleen Editor of Admin
    // =====================================================================

    [Theory]
    [InlineData(Roles.Editor)]
    [InlineData(Roles.Admin)]
    public async Task Delete_AlsEditorOfAdmin_Geeft204EnVerwijdertBlogpost(string rol)
    {
        // Arrange
        var bestaand = await TestHelpers.MaakBlogpostAanAsync(_factory);
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsRolAsync(client, rol);

        // Act
        var response = await client.DeleteAsync($"{Url}/{bestaand.Id}");

        // Assert: 204 én (effect) de blogpost bestaat niet meer.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var daarna = await client.GetAsync($"{Url}/{bestaand.Id}");
        Assert.Equal(HttpStatusCode.NotFound, daarna.StatusCode);
    }

    [Fact]
    public async Task Delete_ZonderLogin_Geeft401EnVerwijdertNiets()
    {
        // Arrange
        var bestaand = await TestHelpers.MaakBlogpostAanAsync(_factory);
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync($"{Url}/{bestaand.Id}");

        // Assert: 401 én de blogpost bestaat nog.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var daarna = await client.GetAsync($"{Url}/{bestaand.Id}");
        Assert.Equal(HttpStatusCode.OK, daarna.StatusCode);
    }

    [Fact]
    public async Task Delete_AlsGast_Geeft403EnVerwijdertNiets()
    {
        // Arrange
        var bestaand = await TestHelpers.MaakBlogpostAanAsync(_factory);
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsGastAsync(client);

        // Act
        var response = await client.DeleteAsync($"{Url}/{bestaand.Id}");

        // Assert: 403 én de blogpost bestaat nog.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var daarna = await client.GetAsync($"{Url}/{bestaand.Id}");
        Assert.Equal(HttpStatusCode.OK, daarna.StatusCode);
    }

    [Fact]
    public async Task Delete_OnbekendId_Geeft404()
    {
        // Arrange
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsEditorAsync(client);

        // Act
        var response = await client.DeleteAsync($"{Url}/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

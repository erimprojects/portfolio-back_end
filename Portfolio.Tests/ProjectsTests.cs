using System.Net;                // HttpStatusCode
using System.Net.Http.Json;      // PostAsJsonAsync, PutAsJsonAsync, ReadFromJsonAsync
using portfolio_backend.Auth;    // Roles (voor [InlineData])
using portfolio_backend.Dtos;

namespace Portfolio.Tests;

// Integratietests via HTTP voor alle 5 projectroutes.
// IClassFixture<PortfolioApiFactory>: xUnit maakt één test-API met één lege SQLite-database
// voor alle tests in deze klasse en geeft die via de constructor mee.
// Elke test maakt zijn eigen HttpClient (= eigen "browser"), dus begint niet ingelogd.
public class ProjectsTests : IClassFixture<PortfolioApiFactory>
{
    private const string Url = "/api/projects";
    private readonly PortfolioApiFactory _factory;

    public ProjectsTests(PortfolioApiFactory factory)
    {
        _factory = factory;
    }

    // =====================================================================
    // GET /api/projects en GET /api/projects/{id}  –  publiek (iedereen)
    // =====================================================================

    [Fact]
    public async Task GetAll_ZonderLogin_Geeft200EnBevatBestaandProject()
    {
        // Arrange: er staat een project in de database, client is niet ingelogd.
        var bestaand = await TestHelpers.MaakProjectAanAsync(_factory);
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(Url);

        // Assert: statuscode én inhoud (het project zit in de lijst).
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projecten = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>();
        Assert.NotNull(projecten);
        Assert.Contains(projecten, p => p.Id == bestaand.Id && p.Titel == bestaand.Titel);
    }

    [Fact]
    public async Task GetById_BestaandId_Geeft200EnJuisteGegevens()
    {
        // Arrange
        var bestaand = await TestHelpers.MaakProjectAanAsync(_factory);
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"{Url}/{bestaand.Id}");

        // Assert: ProjectResponse is een record, dus Assert.Equal vergelijkt ALLE velden.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var gevonden = await response.Content.ReadFromJsonAsync<ProjectResponse>();
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
    // POST /api/projects  –  alleen Editor of Admin
    // =====================================================================

    // [Theory] + [InlineData]: dezelfde test draait twee keer, als Editor én als Admin.
    [Theory]
    [InlineData(Roles.Editor)]
    [InlineData(Roles.Admin)]
    public async Task Post_AlsEditorOfAdmin_Geeft201EnSlaatProjectOp(string rol)
    {
        // Arrange
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsRolAsync(client, rol);
        var nieuw = TestHelpers.NieuwProject(TestHelpers.UniekeTitel("Nieuw project"));

        // Act
        var response = await client.PostAsJsonAsync(Url, nieuw);

        // Assert 1: 201 Created + Location-header naar het nieuwe project.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        // Assert 2: de response bevat de verstuurde gegevens + een nieuw Id van de database.
        var aangemaakt = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(aangemaakt);
        Assert.True(aangemaakt.Id > 0);
        Assert.Equal(nieuw.Titel, aangemaakt.Titel);
        Assert.Equal(nieuw.GitHubUrl, aangemaakt.GitHubUrl);

        // Assert 3 (effect): het project is echt opgeslagen en via GET terug te vinden.
        var opgehaald = await client.GetFromJsonAsync<ProjectResponse>($"{Url}/{aangemaakt.Id}");
        Assert.Equal(aangemaakt, opgehaald);
    }

    [Fact]
    public async Task Post_ZonderLogin_Geeft401EnSlaatNietsOp()
    {
        // Arrange: niet ingelogd.
        var client = _factory.CreateClient();
        var nieuw = TestHelpers.NieuwProject(TestHelpers.UniekeTitel("Geweigerd project"));

        // Act
        var response = await client.PostAsJsonAsync(Url, nieuw);

        // Assert: 401 én het project staat NIET in de database.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var projecten = await client.GetFromJsonAsync<List<ProjectResponse>>(Url);
        Assert.DoesNotContain(projecten!, p => p.Titel == nieuw.Titel);
    }

    [Fact]
    public async Task Post_AlsGast_Geeft403EnSlaatNietsOp()
    {
        // Arrange: ingelogd, maar alleen met de rol Gast.
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsGastAsync(client);
        var nieuw = TestHelpers.NieuwProject(TestHelpers.UniekeTitel("Geweigerd project"));

        // Act
        var response = await client.PostAsJsonAsync(Url, nieuw);

        // Assert: 403 én het project staat NIET in de database.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var projecten = await client.GetFromJsonAsync<List<ProjectResponse>>(Url);
        Assert.DoesNotContain(projecten!, p => p.Titel == nieuw.Titel);
    }

    // Ongeldige invoer: [Required] en [Url] in ProjectRequest geven 400 nog vóór de service draait.
    [Theory]
    [InlineData("", "https://github.com/erimprojects")]      // lege titel
    [InlineData("   ", "https://github.com/erimprojects")]   // titel met alleen spaties
    [InlineData("Geldige titel", "geen-geldige-link")]       // ongeldige GitHub-link
    public async Task Post_OngeldigeInvoer_Geeft400(string titel, string gitHubUrl)
    {
        // Arrange
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsEditorAsync(client);
        var ongeldig = TestHelpers.NieuwProject(titel, gitHubUrl);

        // Act
        var response = await client.PostAsJsonAsync(Url, ongeldig);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =====================================================================
    // PUT /api/projects/{id}  –  alleen Editor of Admin
    // =====================================================================

    [Theory]
    [InlineData(Roles.Editor)]
    [InlineData(Roles.Admin)]
    public async Task Put_AlsEditorOfAdmin_Geeft204EnWijzigtProject(string rol)
    {
        // Arrange
        var bestaand = await TestHelpers.MaakProjectAanAsync(_factory);
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsRolAsync(client, rol);
        var wijziging = TestHelpers.NieuwProject(TestHelpers.UniekeTitel("Gewijzigd project"));

        // Act
        var response = await client.PutAsJsonAsync($"{Url}/{bestaand.Id}", wijziging);

        // Assert: 204 én (effect) de nieuwe titel staat in de database.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var opgehaald = await client.GetFromJsonAsync<ProjectResponse>($"{Url}/{bestaand.Id}");
        Assert.Equal(wijziging.Titel, opgehaald!.Titel);
    }

    [Fact]
    public async Task Put_ZonderLogin_Geeft401EnWijzigtNiets()
    {
        // Arrange
        var bestaand = await TestHelpers.MaakProjectAanAsync(_factory);
        var client = _factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync($"{Url}/{bestaand.Id}", TestHelpers.NieuwProject("Mag niet"));

        // Assert: 401 én de oude titel staat er nog.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var opgehaald = await client.GetFromJsonAsync<ProjectResponse>($"{Url}/{bestaand.Id}");
        Assert.Equal(bestaand.Titel, opgehaald!.Titel);
    }

    [Fact]
    public async Task Put_AlsGast_Geeft403EnWijzigtNiets()
    {
        // Arrange
        var bestaand = await TestHelpers.MaakProjectAanAsync(_factory);
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsGastAsync(client);

        // Act
        var response = await client.PutAsJsonAsync($"{Url}/{bestaand.Id}", TestHelpers.NieuwProject("Mag niet"));

        // Assert: 403 én de oude titel staat er nog.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var opgehaald = await client.GetFromJsonAsync<ProjectResponse>($"{Url}/{bestaand.Id}");
        Assert.Equal(bestaand.Titel, opgehaald!.Titel);
    }

    [Fact]
    public async Task Put_OnbekendId_Geeft404()
    {
        // Arrange
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsEditorAsync(client);

        // Act
        var response = await client.PutAsJsonAsync($"{Url}/999999", TestHelpers.NieuwProject());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_OngeldigeInvoer_Geeft400EnWijzigtNiets()
    {
        // Arrange
        var bestaand = await TestHelpers.MaakProjectAanAsync(_factory);
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsEditorAsync(client);

        // Act: lege titel.
        var response = await client.PutAsJsonAsync($"{Url}/{bestaand.Id}", TestHelpers.NieuwProject(""));

        // Assert: 400 én de oude titel staat er nog.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var opgehaald = await client.GetFromJsonAsync<ProjectResponse>($"{Url}/{bestaand.Id}");
        Assert.Equal(bestaand.Titel, opgehaald!.Titel);
    }

    // =====================================================================
    // DELETE /api/projects/{id}  –  alleen Editor of Admin
    // =====================================================================

    [Theory]
    [InlineData(Roles.Editor)]
    [InlineData(Roles.Admin)]
    public async Task Delete_AlsEditorOfAdmin_Geeft204EnVerwijdertProject(string rol)
    {
        // Arrange
        var bestaand = await TestHelpers.MaakProjectAanAsync(_factory);
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsRolAsync(client, rol);

        // Act
        var response = await client.DeleteAsync($"{Url}/{bestaand.Id}");

        // Assert: 204 én (effect) het project bestaat niet meer.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var daarna = await client.GetAsync($"{Url}/{bestaand.Id}");
        Assert.Equal(HttpStatusCode.NotFound, daarna.StatusCode);
    }

    [Fact]
    public async Task Delete_ZonderLogin_Geeft401EnVerwijdertNiets()
    {
        // Arrange
        var bestaand = await TestHelpers.MaakProjectAanAsync(_factory);
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync($"{Url}/{bestaand.Id}");

        // Assert: 401 én het project bestaat nog.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var daarna = await client.GetAsync($"{Url}/{bestaand.Id}");
        Assert.Equal(HttpStatusCode.OK, daarna.StatusCode);
    }

    [Fact]
    public async Task Delete_AlsGast_Geeft403EnVerwijdertNiets()
    {
        // Arrange
        var bestaand = await TestHelpers.MaakProjectAanAsync(_factory);
        var client = _factory.CreateClient();
        await TestHelpers.LoginAlsGastAsync(client);

        // Act
        var response = await client.DeleteAsync($"{Url}/{bestaand.Id}");

        // Assert: 403 én het project bestaat nog.
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

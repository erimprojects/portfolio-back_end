using Microsoft.EntityFrameworkCore; // EF-methodes: ToListAsync, FirstOrDefaultAsync, AsNoTracking
using portfolio_backend.Data;        // PortfolioDb (de DbContext)
using portfolio_backend.Dtos;        // BlogpostRequest en BlogpostResponse
using portfolio_backend.Models;      // de entity Blogpost

namespace portfolio_backend.Services;

// Service = logica-laag. Zit tussen de controller (HTTP) en de DbContext (database).
// Hier gebeurt ook de omzetting (mapping) tussen entity en DTO.
public class BlogpostService : IBlogpostService
{
    // Veld waarin de DbContext bewaard wordt
    // readonly = wordt één keer ingevuld in de constructor en daarna nooit meer veranderd
    private readonly PortfolioDb _db;

    // Constructorinjectie: de service VRAAGT om een PortfolioDb,
    // DI geeft hem die (geregistreerd in Program.cs met AddDbContext).
    // Nergens staat "new PortfolioDb(...)".
    public BlogpostService(PortfolioDb db)
    {
        _db = db; // bewaar de DbContext zodat alle methodes hem kunnen gebruiken
    }

    // GET alle blogposts
    public async Task<IReadOnlyList<BlogpostResponse>> GetAllAsync(CancellationToken ct)
    {
        var blogposts = await _db.Blogposts // de tabel Blogposts
            .AsNoTracking()                 // EF houdt de blogposts niet bij: sneller bij alleen lezen
            .OrderBy(b => b.Id)             // sorteer op id, zodat de volgorde altijd hetzelfde is
            .ToListAsync(ct);               // hier gaat de query echt naar SQL Server (SELECT)

        // Zet elke entity (Blogpost) om naar een DTO (BlogpostResponse) met ToResponse
        return blogposts.Select(ToResponse).ToList();
    }

    // GET één blogpost op id
    public async Task<BlogpostResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var blogpost = await _db.Blogposts
            .AsNoTracking()                               // alleen lezen
            .FirstOrDefaultAsync(b => b.Id == id, ct);    // zoek de blogpost met dit id, of null

        // Niet gevonden? -> null (de controller maakt er een 404 van)
        // Wel gevonden?  -> omzetten naar DTO
        return blogpost is null ? null : ToResponse(blogpost);
    }

    // POST: nieuwe blogpost maken
    public async Task<BlogpostResponse> CreateAsync(BlogpostRequest request, CancellationToken ct)
    {
        // Mapping DTO -> entity: maak een nieuwe Blogpost met de waarden uit de request
        // GEEN Id invullen: dat doet de database
        var blogpost = new Blogpost
        {
            Titel = request.Titel,
            Inhoud = request.Inhoud,
            Publicatiedatum = request.Publicatiedatum
        };

        _db.Blogposts.Add(blogpost);     // markeer als nieuw (nog niet opgeslagen)
        await _db.SaveChangesAsync(ct);  // nu echt opslaan in SQL Server (INSERT)

        // Na het opslaan heeft blogpost.Id een waarde gekregen van de database
        return ToResponse(blogpost);     // geef de nieuwe blogpost terug als DTO, met id
    }

    // PUT: blogpost wijzigen
    public async Task<bool> UpdateAsync(int id, BlogpostRequest request, CancellationToken ct)
    {
        // Laden ZONDER AsNoTracking: EF moet de blogpost volgen om de wijziging te zien
        var blogpost = await _db.Blogposts.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (blogpost is null)
            return false; // niet gevonden -> controller geeft 404

        // Mapping DTO -> entity: zet de nieuwe waarden uit de request in de entity
        blogpost.Titel = request.Titel;
        blogpost.Inhoud = request.Inhoud;
        blogpost.Publicatiedatum = request.Publicatiedatum;

        await _db.SaveChangesAsync(ct); // EF ziet wat veranderd is en stuurt een UPDATE
        return true;                    // gelukt -> controller geeft 204
    }

    // DELETE: blogpost verwijderen
    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        // Laden ZONDER AsNoTracking: EF moet weten welke rij weg moet
        var blogpost = await _db.Blogposts.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (blogpost is null)
            return false; // niet gevonden -> controller geeft 404

        _db.Blogposts.Remove(blogpost); // markeer als "moet weg"
        await _db.SaveChangesAsync(ct); // nu echt verwijderen in SQL Server (DELETE)
        return true;                    // gelukt -> controller geeft 204
    }

    // Mapping entity -> DTO, op een plek 
    // private = alleen deze service gebruikt hem
    // static  = heeft niks van de service zelf nodig, alleen de blogpost die je meegeeft
    // Volgorde moet gelijk zijn aan het record BlogpostResponse: Id, Titel, Inhoud, Publicatiedatum
    private static BlogpostResponse ToResponse(Blogpost b) =>
        new(b.Id, b.Titel, b.Inhoud, b.Publicatiedatum);
}
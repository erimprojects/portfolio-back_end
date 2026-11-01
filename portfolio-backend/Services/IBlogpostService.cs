using portfolio_backend.Dtos; // nodig voor BlogpostRequest en BlogpostResponse

namespace portfolio_backend.Services;

// Interface = contract: welke methodes de service MOET hebben (niet HOE ze werken)
// De controller kent alleen deze interface, niet de echte BlogpostService
public interface IBlogpostService
{
    // Alle blogposts ophalen -> later een lijst met DTO's
    Task<IReadOnlyList<BlogpostResponse>> GetAllAsync(CancellationToken ct);

    // Eén blogpost ophalen -> DTO, of null als hij niet bestaat (? = mag null zijn)
    Task<BlogpostResponse?> GetByIdAsync(int id, CancellationToken ct);

    // Nieuwe blogpost maken -> geeft de nieuwe DTO terug, MET het id van de database
    Task<BlogpostResponse> CreateAsync(BlogpostRequest request, CancellationToken ct);

    // Blogpost wijzigen -> true = gelukt, false = niet gevonden
    Task<bool> UpdateAsync(int id, BlogpostRequest request, CancellationToken ct);

    // Blogpost verwijderen -> true = gelukt, false = niet gevonden
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
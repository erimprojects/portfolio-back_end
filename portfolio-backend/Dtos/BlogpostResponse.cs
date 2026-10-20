namespace portfolio_backend.Dtos;

public record BlogpostResponse(
    int Id,
    string Titel,
    string Inhoud,
    DateTime Publicatiedatum);

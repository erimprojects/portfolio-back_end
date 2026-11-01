namespace portfolio_backend.Dtos;

public class BlogpostRequest
{
    public string Titel { get; init; } = string.Empty;
    public string Inhoud { get; init; } = string.Empty;
    public DateTime Publicatiedatum { get; init; }
}

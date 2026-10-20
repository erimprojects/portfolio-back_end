namespace portfolio_backend.Dtos;

public class ProjectRequest
{
    public string Titel { get; init; } = string.Empty;
    public string Beschrijving { get; init; } = string.Empty;
    public string Categorie { get; init; } = string.Empty;
    public string GitHubUrl { get; init; } = string.Empty;
    public DateTime Datum { get; init; }
}
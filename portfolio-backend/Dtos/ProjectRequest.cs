using System.ComponentModel.DataAnnotations; // [Required], [StringLength], [Url]
namespace portfolio_backend.Dtos;

public class ProjectRequest
{
    [Required, StringLength(100)]
    public string Titel { get; init; } = string.Empty;

    [Required] // mag niet leeg zijn 
    public string Beschrijving { get; init; } = string.Empty;

    [Required]
    public string Categorie { get; init; } = string.Empty;

    [Url] // dit checkt voor geldige link
    public string GitHubUrl { get; init; } = string.Empty;
    public DateTime Datum { get; init; }
}
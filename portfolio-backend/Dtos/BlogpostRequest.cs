using System.ComponentModel.DataAnnotations;
namespace portfolio_backend.Dtos;

public class BlogpostRequest
{
    [Required, StringLength(100)]
    public string Titel { get; init; } = string.Empty;

    [Required]
    public string Inhoud { get; init; } = string.Empty;
    public DateTime Publicatiedatum { get; init; }
}

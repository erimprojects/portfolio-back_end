using Microsoft.EntityFrameworkCore;
using portfolio_backend.Data;
using portfolio_backend.Dtos;
using portfolio_backend.Models;

namespace portfolio_backend.Services;

public class ProjectService : IProjectService
{
    private readonly PortfolioDb _db;

    public ProjectService(PortfolioDb db)
    {
        _db = db; // _db bewaart de database context die wordt geïnjecteerd via dependency injection
    }

    public async Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken ct)
    {
        var projects = await _db.Projects
            .AsNoTracking() // zorgt dat je alleen leest en niet per ongeluk iets aanpast
            .OrderBy(p => p.Id) // sorteert de projecten op id
            .ToListAsync(ct);

        return projects.Select(ToResponse).ToList(); // converteert de lijst van Project naar een lijst van ProjectResponse
    }

    public async Task<ProjectResponse?> GetByIdAsync(int id, CancellationToken ct) // haalt een project op basis van het id
    {
        var project = await _db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return project is null ? null : ToResponse(project); // als het project null is, return null, anders converteer het naar ProjectResponse
    }

    public async Task<ProjectResponse> CreateAsync(ProjectRequest request, CancellationToken ct)
    {
        var project = new Project
        {
            Titel = request.Titel,
            Beschrijving = request.Beschrijving,
            Categorie = request.Categorie,
            GitHubUrl = request.GitHubUrl,
            Datum = request.Datum
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        return ToResponse(project);
    }

    public async Task<bool> UpdateAsync(int id, ProjectRequest request, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
            return false;

        project.Titel = request.Titel;
        project.Beschrijving = request.Beschrijving;
        project.Categorie = request.Categorie;
        project.GitHubUrl = request.GitHubUrl;
        project.Datum = request.Datum;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
            return false;

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(ct);
        return true;
    }
    // Mapping: entity -> DTO (op één plek)
    private static ProjectResponse ToResponse(Project p) =>
        new(p.Id, p.Titel, p.Beschrijving, p.Categorie, p.GitHubUrl, p.Datum);

}
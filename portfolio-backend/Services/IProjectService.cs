using portfolio_backend.Dtos;

namespace portfolio_backend.Services;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken ct);
    Task<ProjectResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ProjectResponse> CreateAsync(ProjectRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, ProjectRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
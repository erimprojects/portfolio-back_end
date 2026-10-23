using Microsoft.AspNetCore.Mvc;
using portfolio_backend.Dtos;
using portfolio_backend.Services;

namespace portfolio_backend.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _service;

    public ProjectController(IProjectService service) // Dependency Injection Contructor
    {
        _service = service;
    }
    // get api/projects
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectResponse>>> GetAll(CancellationToken ct
)
    {
        var projects = await _service.GetAllAsync(ct);
        return Ok(projects);
    }

    // get api/projects/5
    [HttpGet("{id:int}")] // getal komt in de parameter
    public async Task<ActionResult<ProjectResponse>> GetById(int id, CancellationToken ct) // geeft een project response terug
    {
        var project = await _service.GetByIdAsync(id, ct); // vraagt om project met id
        if (project is null)
            return NotFound();

        return Ok(project);
    }
}
using Microsoft.AspNetCore.Mvc;
using portfolio_backend.Dtos;
using portfolio_backend.Services;

namespace portfolio_backend.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _service;

    public ProjectsController(IProjectService service) // Dependency Injection Contructor
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
        // POST api/projects
    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(ProjectRequest request, CancellationToken ct)
    {
        var project = await _service.CreateAsync(request, ct); // service maakt het project aan in de database
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project); // 201 Created + Location-header
    }

    // PUT api/projects/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProjectRequest request, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(id, request, ct); // true = gelukt, false = niet gevonden
        if (!updated)
            return NotFound(); // 404

        return NoContent(); // 204
    }

    // DELETE api/projects/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct); // true = gelukt, false = niet gevonden
        if (!deleted)
            return NotFound(); // 404

        return NoContent(); // 204
    }
}
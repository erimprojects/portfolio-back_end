using Microsoft.AspNetCore.Mvc;
using portfolio_backend.Dtos;
using portfolio_backend.Services;
using Microsoft.AspNetCore.Authorization; // hier zit [Authorize] in
using portfolio_backend.Auth;             // Policies
namespace portfolio_backend.Controllers;

[ApiController]
[Route("api/blogposts")]
public class BlogpostsController : ControllerBase
{
    private readonly BlogpostService _service;

    public BlogpostsController(BlogpostService service)
    {
        _service = service;
    }

    // GET api/blogposts
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BlogpostResponse>>> GetAll(CancellationToken ct)// actionresult<projectresponse> = zorgt ervoor dat de methode data en een statuscode teruggeeft.
    {
        var blogposts = await _service.GetAllAsync(ct);
        return Ok(blogposts);
    }

    // GET api/blogposts/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BlogpostResponse>> GetById(int id, CancellationToken ct)
    {
        var blogpost = await _service.GetByIdAsync(id, ct);
        if (blogpost is null)
            return NotFound();

        return Ok(blogpost);
    }

    // POST api/blogposts
    [HttpPost]
    [Authorize(Policy = Policies.KanSchrijven)] // zo zie je dat admin of editor alleen bevoegt is dit zit bij elke post, put, delete.
    public async Task<ActionResult<BlogpostResponse>> Create(BlogpostRequest request, CancellationToken ct)
    {
        var blogpost = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = blogpost.Id }, blogpost);
    }

    // PUT api/blogposts/5
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.KanSchrijven)] // zo zie je dat admin of editor alleen bevoegt is dit zit bij elke post, put, delete.
    public async Task<IActionResult> Update(int id, BlogpostRequest request, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(id, request, ct);
        if (!updated)
            return NotFound();

        return NoContent();
    }

    // DELETE api/blogposts/5
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.KanSchrijven)] // zo zie je dat admin of editor alleen bevoegt is dit zit bij elke post, put, delete.

    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
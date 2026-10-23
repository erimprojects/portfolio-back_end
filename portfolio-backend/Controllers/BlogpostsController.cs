using Microsoft.AspNetCore.Mvc;
using portfolio_backend.Dtos;
using portfolio_backend.Services;

namespace portfolio_backend.Controllers;

[ApiController]
[Route("api/blogposts")]
public class BlogpostsController : ControllerBase
{
    private readonly IBlogpostService _service;

    public BlogpostsController(IBlogpostService service)
    {
        _service = service;
    }

    // GET api/blogposts
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BlogpostResponse>>> GetAll(CancellationToken ct)
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
    public async Task<ActionResult<BlogpostResponse>> Create(BlogpostRequest request, CancellationToken ct)
    {
        var blogpost = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = blogpost.Id }, blogpost);
    }

    // PUT api/blogposts/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, BlogpostRequest request, CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(id, request, ct);
        if (!updated)
            return NotFound();

        return NoContent();
    }

    // DELETE api/blogposts/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
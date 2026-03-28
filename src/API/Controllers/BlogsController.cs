using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SphereBlog.API.Models;
using SphereBlog.Application.DTOs.Blogs;
using SphereBlog.Application.Interfaces;

namespace SphereBlog.API.Controllers;

[ApiController]
[Route("api/blogs")]
public class BlogsController(IBlogService blogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedData<BlogResponse>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(page, 1);

        var result = await blogService.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedData<BlogResponse>>.Success(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BlogResponse>), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await blogService.GetByIdAsync(id);
        return Ok(ApiResponse<BlogResponse>.Success(result));
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BlogResponse>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateBlogRequest request)
    {
        var userId = GetUserId();
        var result = await blogService.CreateAsync(request, userId);
        return StatusCode(201, ApiResponse<BlogResponse>.Success(result, 201));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BlogResponse>), 200)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBlogRequest request)
    {
        var userId = GetUserId();
        var result = await blogService.UpdateAsync(id, request, userId);
        return Ok(ApiResponse<BlogResponse>.Success(result));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        await blogService.DeleteAsync(id, userId);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(claim!);
    }
}

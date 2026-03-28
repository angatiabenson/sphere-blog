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
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await blogService.GetByIdAsync(id);
            return Ok(ApiResponse<BlogResponse>.Success(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Error(ex.Message, 404, CorrelationId));
        }
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
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBlogRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await blogService.UpdateAsync(id, request, userId);
            return Ok(ApiResponse<BlogResponse>.Success(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Error(ex.Message, 404, CorrelationId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse<object>.Error("You do not have permission to perform this action.", 403, CorrelationId));
        }
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetUserId();
            await blogService.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Error(ex.Message, 404, CorrelationId));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse<object>.Error("You do not have permission to perform this action.", 403, CorrelationId));
        }
    }

    private Guid GetUserId()
    {
        var value = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (value is null)
            throw new UnauthorizedAccessException();
        return Guid.Parse(value);
    }

    private string CorrelationId =>
        HttpContext.Items["CorrelationId"]?.ToString() ?? "unknown";
}

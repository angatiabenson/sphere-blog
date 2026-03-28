using Microsoft.AspNetCore.Mvc;
using SphereBlog.API.Models;
using SphereBlog.Application.DTOs.Blogs;
using SphereBlog.Application.DTOs.Tags;
using SphereBlog.Application.Interfaces;

namespace SphereBlog.API.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController(ITagService tagService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TagResponse>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var result = await tagService.GetAllAsync();
        return Ok(ApiResponse<List<TagResponse>>.Success(result));
    }

    [HttpGet("{name}/blogs")]
    [ProducesResponseType(typeof(ApiResponse<PagedData<BlogResponse>>), 200)]
    public async Task<IActionResult> GetBlogsByTag(string name, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(page, 1);

        var result = await tagService.GetBlogsByTagAsync(name, page, pageSize);
        return Ok(ApiResponse<PagedData<BlogResponse>>.Success(result));
    }
}

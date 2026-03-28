using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SphereBlog.API.Models;
using SphereBlog.Application.DTOs.Comments;
using SphereBlog.Application.Interfaces;

namespace SphereBlog.API.Controllers;

[ApiController]
[Route("api/blogs/{blogId:guid}/comments")]
[Authorize]
public class CommentsController(ICommentService commentService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CommentResponse>), 201)]
    public async Task<IActionResult> Create(Guid blogId, [FromBody] CreateCommentRequest request)
    {
        var userId = GetUserId();
        var result = await commentService.CreateAsync(blogId, request, userId);
        return StatusCode(201, ApiResponse<CommentResponse>.Success(result, 201));
    }

    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid blogId, Guid commentId)
    {
        var userId = GetUserId();
        await commentService.DeleteAsync(blogId, commentId, userId);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(claim!);
    }
}

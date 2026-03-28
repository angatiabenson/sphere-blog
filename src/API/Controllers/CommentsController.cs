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
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Create(Guid blogId, [FromBody] CreateCommentRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await commentService.CreateAsync(blogId, request, userId);
            return StatusCode(201, ApiResponse<CommentResponse>.Success(result, 201));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Error(ex.Message, 404, CorrelationId));
        }
    }

    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Delete(Guid blogId, Guid commentId)
    {
        try
        {
            var userId = GetUserId();
            await commentService.DeleteAsync(blogId, commentId, userId);
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

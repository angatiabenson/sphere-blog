using SphereBlog.Application.DTOs.Comments;

namespace SphereBlog.Application.Interfaces;

public interface ICommentService
{
    Task<CommentResponse> CreateAsync(Guid blogId, CreateCommentRequest request, Guid userId);
    Task DeleteAsync(Guid blogId, Guid commentId, Guid userId);
}

using Microsoft.EntityFrameworkCore;
using SphereBlog.Application.DTOs.Comments;
using SphereBlog.Application.Interfaces;
using SphereBlog.Domain.Entities;
using SphereBlog.Infrastructure.Data;

namespace SphereBlog.Application.Services;

public class CommentService(AppDbContext db) : ICommentService
{
    public async Task<CommentResponse> CreateAsync(
        Guid blogId,
        CreateCommentRequest request,
        Guid userId
    )
    {
        var blogExists = await db.Blogs.AnyAsync(b => b.Id == blogId);
        if (!blogExists)
            throw new KeyNotFoundException("Blog not found.");

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow,
            BlogId = blogId,
            AuthorId = userId,
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        var author = await db.Users.FindAsync(userId);

        return new CommentResponse
        {
            Id = comment.Id,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt,
            AuthorId = comment.AuthorId,
            AuthorName = author!.DisplayName,
        };
    }

    public async Task DeleteAsync(Guid blogId, Guid commentId, Guid userId)
    {
        var comment = await db.Comments.FirstOrDefaultAsync(c =>
            c.Id == commentId && c.BlogId == blogId
        );

        if (comment is null)
            throw new KeyNotFoundException("Comment not found.");

        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException();

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
    }
}

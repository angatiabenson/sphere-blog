using SphereBlog.Application.DTOs.Comments;
using SphereBlog.Application.DTOs.Tags;

namespace SphereBlog.Application.DTOs.Blogs;

public class BlogResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public List<TagResponse> Tags { get; set; } = [];
    public List<CommentResponse> Comments { get; set; } = [];
}

namespace SphereBlog.Domain.Entities;

public class Blog
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public ICollection<BlogTag> BlogTags { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
}

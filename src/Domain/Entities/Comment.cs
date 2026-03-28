namespace SphereBlog.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Guid BlogId { get; set; }
    public Blog Blog { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
}

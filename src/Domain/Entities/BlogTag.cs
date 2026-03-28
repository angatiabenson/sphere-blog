namespace SphereBlog.Domain.Entities;

public class BlogTag
{
    public Guid BlogId { get; set; }
    public Blog Blog { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}

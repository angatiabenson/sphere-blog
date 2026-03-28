namespace SphereBlog.Domain.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<BlogTag> BlogTags { get; set; } = [];
}

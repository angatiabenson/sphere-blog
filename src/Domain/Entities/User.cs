namespace SphereBlog.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<Blog> Blogs { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
}

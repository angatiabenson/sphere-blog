using System.ComponentModel.DataAnnotations;

namespace SphereBlog.Application.DTOs.Blogs;

public class UpdateBlogRequest
{
    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public List<string> TagNames { get; set; } = [];
}

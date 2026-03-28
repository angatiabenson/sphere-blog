using System.ComponentModel.DataAnnotations;

namespace SphereBlog.Application.DTOs.Comments;

public class CreateCommentRequest
{
    [Required]
    [MaxLength(2000)]
    public string Body { get; set; } = string.Empty;
}

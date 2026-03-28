using SphereBlog.API.Models;
using SphereBlog.Application.DTOs.Blogs;
using SphereBlog.Application.DTOs.Tags;

namespace SphereBlog.Application.Interfaces;

public interface ITagService
{
    Task<List<TagResponse>> GetAllAsync();
    Task<PagedData<BlogResponse>> GetBlogsByTagAsync(string tagName, int page, int pageSize);
}

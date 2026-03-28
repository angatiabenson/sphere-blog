using SphereBlog.API.Models;
using SphereBlog.Application.DTOs.Blogs;

namespace SphereBlog.Application.Interfaces;

public interface IBlogService
{
    Task<PagedData<BlogResponse>> GetAllAsync(int page, int pageSize);
    Task<BlogResponse> GetByIdAsync(Guid id);
    Task<BlogResponse> CreateAsync(CreateBlogRequest request, Guid userId);
    Task<BlogResponse> UpdateAsync(Guid id, UpdateBlogRequest request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

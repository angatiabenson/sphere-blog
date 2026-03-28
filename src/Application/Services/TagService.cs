using Microsoft.EntityFrameworkCore;
using SphereBlog.API.Models;
using SphereBlog.Application.DTOs.Blogs;
using SphereBlog.Application.DTOs.Tags;
using SphereBlog.Application.Interfaces;
using SphereBlog.Infrastructure.Data;

namespace SphereBlog.Application.Services;

public class TagService(AppDbContext db) : ITagService
{
    public async Task<List<TagResponse>> GetAllAsync()
    {
        return await db
            .Tags.OrderBy(t => t.Name)
            .Select(t => new TagResponse { Id = t.Id, Name = t.Name })
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PagedData<BlogResponse>> GetBlogsByTagAsync(
        string tagName,
        int page,
        int pageSize
    )
    {
        var normalized = tagName.Trim().ToLowerInvariant();

        var query = db
            .Blogs.Include(b => b.Author)
            .Include(b => b.BlogTags)
                .ThenInclude(bt => bt.Tag)
            .Where(b => b.BlogTags.Any(bt => bt.Tag.Name == normalized))
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var blogs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedData<BlogResponse>
        {
            Items = blogs.Select(b => BlogService.MapToResponse(b, includeComments: false)),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}

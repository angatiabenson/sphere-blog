using Microsoft.EntityFrameworkCore;
using SphereBlog.API.Models;
using SphereBlog.Application.DTOs.Blogs;
using SphereBlog.Application.DTOs.Comments;
using SphereBlog.Application.DTOs.Tags;
using SphereBlog.Application.Interfaces;
using SphereBlog.Domain.Entities;
using SphereBlog.Infrastructure.Data;

namespace SphereBlog.Application.Services;

public class BlogService(AppDbContext db) : IBlogService
{
    public async Task<PagedData<BlogResponse>> GetAllAsync(int page, int pageSize)
    {
        var query = db
            .Blogs.Include(b => b.Author)
            .Include(b => b.BlogTags)
                .ThenInclude(bt => bt.Tag)
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var blogs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedData<BlogResponse>
        {
            Items = blogs.Select(b => MapToResponse(b, includeComments: false)),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<BlogResponse> GetByIdAsync(Guid id)
    {
        var blog = await db.Blogs
            .Include(b => b.Author)
            .Include(b => b.BlogTags).ThenInclude(bt => bt.Tag)
            .Include(b => b.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (blog is null)
            throw new KeyNotFoundException("Blog not found.");

        return MapToResponse(blog, includeComments: true);
    }

    public async Task<BlogResponse> CreateAsync(CreateBlogRequest request, Guid userId)
    {
        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            AuthorId = userId,
        };

        blog.BlogTags = await ResolveTags(request.TagNames);

        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        // Detach all tracked entities so the re-query loads fresh data with includes
        db.ChangeTracker.Clear();
        return await GetByIdAsync(blog.Id);
    }

    public async Task<BlogResponse> UpdateAsync(Guid id, UpdateBlogRequest request, Guid userId)
    {
        var blog = await db.Blogs.Include(b => b.BlogTags).FirstOrDefaultAsync(b => b.Id == id);

        if (blog is null)
            throw new KeyNotFoundException("Blog not found.");

        if (blog.AuthorId != userId)
            throw new UnauthorizedAccessException();

        blog.Title = request.Title.Trim();
        blog.Content = request.Content;
        blog.UpdatedAt = DateTime.UtcNow;

        blog.BlogTags.Clear();
        blog.BlogTags = await ResolveTags(request.TagNames);

        await db.SaveChangesAsync();

        return await GetByIdAsync(blog.Id);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var blog = await db.Blogs.FirstOrDefaultAsync(b => b.Id == id);

        if (blog is null)
            throw new KeyNotFoundException("Blog not found.");

        if (blog.AuthorId != userId)
            throw new UnauthorizedAccessException();

        db.Blogs.Remove(blog);
        await db.SaveChangesAsync();
    }

    private async Task<List<BlogTag>> ResolveTags(List<string> tagNames)
    {
        if (tagNames.Count == 0)
            return [];

        var normalized = tagNames
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();

        var existingTags = await db.Tags.Where(t => normalized.Contains(t.Name)).ToListAsync();

        var existingNames = existingTags.Select(t => t.Name).ToHashSet();
        var newTags = normalized
            .Where(n => !existingNames.Contains(n))
            .Select(n => new Tag { Name = n })
            .ToList();

        if (newTags.Count > 0)
        {
            db.Tags.AddRange(newTags);
            await db.SaveChangesAsync();
        }

        var allTags = existingTags.Concat(newTags).ToList();

        return allTags.Select(t => new BlogTag { TagId = t.Id }).ToList();
    }

    internal static BlogResponse MapToResponse(Blog blog, bool includeComments)
    {
        return new BlogResponse
        {
            Id = blog.Id,
            Title = blog.Title,
            Content = blog.Content,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt,
            AuthorId = blog.AuthorId,
            AuthorName = blog.Author.DisplayName,
            Tags = blog
                .BlogTags.Select(bt => new TagResponse { Id = bt.Tag.Id, Name = bt.Tag.Name })
                .ToList(),
            Comments = includeComments
                ? blog
                    .Comments.OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CommentResponse
                    {
                        Id = c.Id,
                        Body = c.Body,
                        CreatedAt = c.CreatedAt,
                        AuthorId = c.AuthorId,
                        AuthorName = c.Author.DisplayName,
                    })
                    .ToList()
                : [],
        };
    }
}

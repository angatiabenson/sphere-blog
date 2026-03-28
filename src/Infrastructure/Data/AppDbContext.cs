using Microsoft.EntityFrameworkCore;
using SphereBlog.Domain.Entities;

namespace SphereBlog.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<BlogTag> BlogTags => Set<BlogTag>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

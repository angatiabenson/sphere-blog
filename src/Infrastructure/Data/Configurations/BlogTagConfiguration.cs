using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SphereBlog.Domain.Entities;

namespace SphereBlog.Infrastructure.Data.Configurations;

public class BlogTagConfiguration : IEntityTypeConfiguration<BlogTag>
{
    public void Configure(EntityTypeBuilder<BlogTag> builder)
    {
        builder.HasKey(bt => new { bt.BlogId, bt.TagId });

        builder.HasOne(bt => bt.Blog)
            .WithMany(b => b.BlogTags)
            .HasForeignKey(bt => bt.BlogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bt => bt.Tag)
            .WithMany(t => t.BlogTags)
            .HasForeignKey(bt => bt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SphereBlog.Domain.Entities;

namespace SphereBlog.Infrastructure.Data.Configurations;

public class BlogConfiguration : IEntityTypeConfiguration<Blog>
{
    public void Configure(EntityTypeBuilder<Blog> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(b => b.Content)
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.HasIndex(b => b.AuthorId);

        builder.HasOne(b => b.Author)
            .WithMany(u => u.Blogs)
            .HasForeignKey(b => b.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

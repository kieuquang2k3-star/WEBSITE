using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Models;

namespace Mitech.Web.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<NewsCategory> NewsCategories => Set<NewsCategory>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<PageContent> PageContents => Set<PageContent>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<JobPosition> JobPositions => Set<JobPosition>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PageContent>()
            .HasIndex(p => new { p.PageKey, p.Lang })
            .IsUnique();

        builder.Entity<Product>()
            .HasIndex(p => p.SortOrder);

        builder.Entity<JobPosition>()
            .HasIndex(j => j.Slug)
            .IsUnique();

        builder.Entity<JobPosition>()
            .HasIndex(j => j.SortOrder);
    }
}

using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}
    public DbSet<User> Users { get; set; }
    public DbSet<Sdata> Sdata { get; set; }
    public DbSet<Shopify> Shopify { get; set; }
    public DbSet<Scrapper> Scrappers { get; set; }
}
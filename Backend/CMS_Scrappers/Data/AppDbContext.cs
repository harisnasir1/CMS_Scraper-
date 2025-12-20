using CMS_Scrappers.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}
    public DbSet<User> Users { get; set; }
    public DbSet<Sdata> Sdata { get; set; }
    public DbSet<Shopify> Shopify { get; set; }
    public DbSet<Scrapper> Scrappers { get; set; }
    
    public DbSet<ProductImageRecord> ProductImages { get; set; }
    
    public DbSet<ProductVariantRecord> ProductVariants { get; set; }
    
    public DbSet<ProductStoreMapping> ProductStoreMapping { get; set; }
}
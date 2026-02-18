using LaundryApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace LaundryApp.Data;

public class LaundryAppDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<LaundryOrder> Orders { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    public LaundryAppDbContext(DbContextOptions<LaundryAppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LaundryOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserEmail).IsRequired();
            entity.Property(e => e.ServiceType).IsRequired();
            entity.Property(e => e.Address).IsRequired();
            entity.Property(e => e.Status).IsRequired();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserEmail).IsRequired();
            entity.Property(e => e.Action).IsRequired();
            entity.Property(e => e.Entity).IsRequired();
            entity.Property(e => e.Details).IsRequired();
        });
    }
}

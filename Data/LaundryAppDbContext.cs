using LaundryApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace LaundryApp.Data;

public class LaundryAppDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<LaundryOrder> Orders { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<PaymentAttempt> PaymentAttempts { get; set; } = null!;

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
            entity.Property(e => e.PricingType).IsRequired();
            entity.Property(e => e.Address).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.PaymentStatus).IsRequired();
            entity.Property(e => e.ItemsJson).IsRequired();
            entity.Property(e => e.Currency).IsRequired();
            entity.Property(e => e.BagWeightLbs).HasPrecision(10, 2);
            
            // Foreign keys
            entity.HasOne(e => e.PaymentMethod)
                .WithMany()
                .HasForeignKey(e => e.PaymentMethodId)
                .IsRequired(false);
            
            entity.HasOne(e => e.Invoice)
                .WithOne(i => i.Order)
                .HasForeignKey<Invoice>(i => i.OrderId)
                .IsRequired(false);
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserEmail).IsRequired();
            entity.Property(e => e.CardToken).IsRequired();
            entity.Property(e => e.CardLast4).IsRequired();
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.DeliveryFee).HasPrecision(18, 2);
            entity.Property(e => e.Tip).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PaymentAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId);
            
            entity.HasOne(e => e.Invoice)
                .WithMany()
                .HasForeignKey(e => e.InvoiceId)
                .IsRequired(false);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserEmail).IsRequired();
            entity.Property(e => e.Action).IsRequired();
            entity.Property(e => e.Entity).IsRequired();
            entity.Property(e => e.Details).IsRequired();
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.StripeCustomerId).IsRequired();
            entity.Property(e => e.DefaultPaymentMethodId).IsRequired();
        });
    }
}

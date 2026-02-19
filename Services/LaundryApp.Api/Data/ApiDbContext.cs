using LaundryApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LaundryApp.Api.Data;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {
    }

    public DbSet<ApiLaundryOrder> Orders => Set<ApiLaundryOrder>();
    public DbSet<ApiPaymentMethod> PaymentMethods => Set<ApiPaymentMethod>();
    public DbSet<ApiInvoice> Invoices => Set<ApiInvoice>();
    public DbSet<ApiPaymentAttempt> PaymentAttempts => Set<ApiPaymentAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiLaundryOrder>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.UserEmail).IsRequired();
            entity.Property(o => o.ServiceType).IsRequired();
            entity.Property(o => o.Address).IsRequired();
            entity.Property(o => o.Status).IsRequired();
            entity.Property(o => o.PaymentStatus).IsRequired();
        });

        modelBuilder.Entity<ApiPaymentMethod>(entity =>
        {
            entity.ToTable("PaymentMethods");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserEmail).IsRequired();
            entity.Property(e => e.CardToken).IsRequired();
            entity.Property(e => e.CardLast4).IsRequired();
        });

        modelBuilder.Entity<ApiInvoice>(entity =>
        {
            entity.ToTable("Invoices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.DeliveryFee).HasPrecision(18, 2);
            entity.Property(e => e.Tip).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ApiPaymentAttempt>(entity =>
        {
            entity.ToTable("PaymentAttempts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });
    }
}

using dotnet_server._Models;
using dotnet_server._Models.Square;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace dotnet_server._Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<BookingDeposit> BookingDeposits => Set<BookingDeposit>();
    public DbSet<SquareWebhookEvent> SquareWebhookEvents => Set<SquareWebhookEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Consultation>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.PhoneNumber).HasMaxLength(40);
            entity.Property(x => x.Timeline).HasMaxLength(80);
            entity.Property(x => x.Status)
                .HasConversion(new EnumToStringConverter<ConsultationStatus>())
                .HasMaxLength(40);
        });

        builder.Entity<BookingDeposit>(entity =>
        {
            entity.Property(x => x.SquareBookingId).HasMaxLength(120);
            entity.Property(x => x.SquareCustomerId).HasMaxLength(120);
            entity.Property(x => x.SquareLocationId).HasMaxLength(120);
            entity.Property(x => x.SquareOrderId).HasMaxLength(120);
            entity.Property(x => x.SquareInvoiceId).HasMaxLength(120);
            entity.Property(x => x.Status)
                .HasConversion(new EnumToStringConverter<BookingDepositStatus>())
                .HasMaxLength(40);
            entity.HasIndex(x => x.SquareBookingId).IsUnique();
            entity.HasIndex(x => x.SquareInvoiceId).IsUnique().HasFilter("\"SquareInvoiceId\" IS NOT NULL");
        });

        builder.Entity<SquareWebhookEvent>(entity =>
        {
            entity.Property(x => x.SquareEventId).HasMaxLength(120);
            entity.Property(x => x.EventType).HasMaxLength(80);
            entity.HasIndex(x => x.SquareEventId).IsUnique();
        });

    }
}

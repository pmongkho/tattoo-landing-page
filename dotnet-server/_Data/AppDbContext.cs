using dotnet_server._Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace dotnet_server._Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Consultation> Consultations => Set<Consultation>();

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
    }
}

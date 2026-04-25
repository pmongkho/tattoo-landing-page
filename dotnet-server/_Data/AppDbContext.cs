using dotnet_server._Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server._Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<ConsultationPreferredDay> ConsultationPreferredDays => Set<ConsultationPreferredDay>();
    public DbSet<TattooDeal> TattooDeals => Set<TattooDeal>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Consultation>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Email).HasMaxLength(180);
            entity.Property(x => x.PhoneNumber).HasMaxLength(40);
            entity.Property(x => x.PreferredArtist).HasMaxLength(120);
            entity.Property(x => x.Style).HasMaxLength(120);
            entity.Property(x => x.Placement).HasMaxLength(120);
            entity.Property(x => x.Size).HasMaxLength(120);
            entity.Property(x => x.Budget).HasMaxLength(120);
            entity.Property(x => x.Description).HasMaxLength(2000);

            entity.HasOne(x => x.TattooDeal)
                .WithMany(x => x.Consultations)
                .HasForeignKey(x => x.TattooDealId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ConsultationPreferredDay>(entity =>
        {
            entity.Property(x => x.Day).HasMaxLength(20);
            entity.HasOne(x => x.Consultation)
                .WithMany(x => x.PreferredDays)
                .HasForeignKey(x => x.ConsultationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TattooDeal>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(150);
            entity.Property(x => x.Style).HasMaxLength(120);
            entity.Property(x => x.Placement).HasMaxLength(120);
            entity.Property(x => x.Size).HasMaxLength(120);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.ReferenceImageUrl).HasMaxLength(2048);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.TattooDeals)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

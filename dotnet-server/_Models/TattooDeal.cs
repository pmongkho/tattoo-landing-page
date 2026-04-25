namespace dotnet_server._Models;

public class TattooDeal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string Placement { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public decimal? OriginalPrice { get; set; }
    public decimal DiscountedPrice { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsFeatured { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedByUser { get; set; } = null!;

    public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
}

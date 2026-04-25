using System.ComponentModel.DataAnnotations;

namespace dotnet_server._Dtos;

public class UpsertTattooDealRequest
{
    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Style { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Placement { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Size { get; set; } = string.Empty;

    public decimal? OriginalPrice { get; set; }

    [Range(0, 999999)]
    public decimal DiscountedPrice { get; set; }

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? ReferenceImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;
    public bool IsFeatured { get; set; }
}

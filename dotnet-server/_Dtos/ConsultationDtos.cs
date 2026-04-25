using System.ComponentModel.DataAnnotations;
using dotnet_server._Models;

namespace dotnet_server._Dtos;

public class CreateConsultationRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? PreferredArtist { get; set; }

    [Required, MaxLength(120)]
    public string Style { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Placement { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Size { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Budget { get; set; }

    public ICollection<string> PreferredDays { get; set; } = new List<string>();

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public bool AgreedToTerms { get; set; }

    public Guid? TattooDealId { get; set; }
}

public class UpdateConsultationStatusRequest
{
    [Required]
    public ConsultationStatus Status { get; set; }
}

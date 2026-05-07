using System.ComponentModel.DataAnnotations;

namespace dotnet_server._Dtos;

public class CreateConsultationRequest
{
    [Required, MaxLength(120)]
    [RegularExpression(@"^\s*\S+(?:\s+\S+)+\s*$", ErrorMessage = "Please provide your first and last name.")]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Timeline { get; set; }
}

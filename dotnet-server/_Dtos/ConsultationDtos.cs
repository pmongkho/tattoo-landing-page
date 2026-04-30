using System.ComponentModel.DataAnnotations;

namespace dotnet_server._Dtos;

public class CreateConsultationRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Timeline { get; set; } = string.Empty;
}

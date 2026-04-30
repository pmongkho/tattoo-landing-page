using System.ComponentModel.DataAnnotations;

namespace dotnet_server._Dtos;

public class UpdateConsultationStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

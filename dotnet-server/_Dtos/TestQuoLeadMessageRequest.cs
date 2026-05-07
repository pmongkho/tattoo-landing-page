using System.ComponentModel.DataAnnotations;

namespace dotnet_server._Dtos;

public class TestQuoLeadMessageRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    public string Timeline { get; set; } = "Not provided";
}

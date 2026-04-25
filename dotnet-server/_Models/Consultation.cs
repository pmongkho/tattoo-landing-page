namespace dotnet_server._Models;

public class Consultation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PreferredArtist { get; set; } = "No preference";
    public string Style { get; set; } = string.Empty;
    public string Placement { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string? Budget { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool AgreedToTerms { get; set; }
    public Guid? TattooDealId { get; set; }
    public TattooDeal? TattooDeal { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ConsultationStatus Status { get; set; } = ConsultationStatus.New;

    public ICollection<ConsultationPreferredDay> PreferredDays { get; set; } = new List<ConsultationPreferredDay>();
}

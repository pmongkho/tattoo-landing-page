namespace dotnet_server._Models;

public class ConsultationPreferredDay
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;
    public string Day { get; set; } = string.Empty;
}

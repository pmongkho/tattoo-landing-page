namespace dotnet_server._Models;

public enum ConsultationStatus
{
    New,
    Contacted,
    ConsultScheduled,
    ConsultCompleted,
    DesignInProgress,
    DepositRequested,
    DepositPaid,
    Booked,
    Completed,
    FollowUp
}

public class Consultation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Timeline { get; set; } = string.Empty;
    public ConsultationStatus Status { get; set; } = ConsultationStatus.New;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

using Microsoft.Extensions.Options;
using dotnet_server._Models;

namespace dotnet_server._Integrations;

public interface IQuoLeadMessagingClient
{
    Task NotifyNewLeadAsync(Consultation consultation, CancellationToken cancellationToken);
}

public interface ISquareBookingClient
{
    Task PrepareBookingWorkflowAsync(Consultation consultation, CancellationToken cancellationToken);
}

public class SquareBookingPlaceholder(ILogger<SquareBookingPlaceholder> logger, IOptions<SquareApiOptions> options)
    : ISquareBookingClient
{
    public Task PrepareBookingWorkflowAsync(Consultation consultation, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[PLACEHOLDER] Square automation not yet implemented. Enabled={Enabled}, LocationId={LocationId}, ConsultationId={ConsultationId}",
            options.Value.Enabled,
            options.Value.LocationId,
            consultation.Id);

        return Task.CompletedTask;
    }
}

using dotnet_server._Data;
using dotnet_server._Dtos;
using dotnet_server._Integrations;
using dotnet_server._Models;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_server._Controllers;

[ApiController]
[Route("api/consultations")]
public class ConsultationsController(
    AppDbContext dbContext,
    IQuoLeadMessagingClient quoLeadMessagingClient,
    ISquareBookingClient squareBookingClient,
    ILogger<ConsultationsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Consultation>> Create([FromBody] CreateConsultationRequest request, CancellationToken cancellationToken)
    {
        var consultation = new Consultation
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Timeline = string.IsNullOrWhiteSpace(request.Timeline) ? "Not provided" : request.Timeline.Trim()
        };

        dbContext.Consultations.Add(consultation);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await quoLeadMessagingClient.NotifyNewLeadAsync(consultation, cancellationToken);
            await squareBookingClient.PrepareBookingWorkflowAsync(consultation, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Placeholder integration step failed for consultation {ConsultationId}", consultation.Id);
        }

        return CreatedAtAction(nameof(Create), new { id = consultation.Id }, consultation);
    }
}

using dotnet_server._Data;
using dotnet_server._Dtos;
using dotnet_server._Integrations;
using dotnet_server._Models;
using dotnet_server._Utils;
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
        var trimmedName = request.Name.Trim();
        if (!HasAtLeastTwoWords(trimmedName))
        {
            return ValidationProblem(detail: "Please provide your first and last name.");
        }

        if (!PhoneNumberNormalizer.TryNormalizeUsPhone(request.PhoneNumber, out var normalizedPhone))
        {
            return ValidationProblem(detail: "Please provide a valid US phone number.");
        }

        var consultation = new Consultation
        {
            Name = trimmedName,
            PhoneNumber = normalizedPhone,
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

    private static bool HasAtLeastTwoWords(string value)
    {
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length >= 2;
    }
}

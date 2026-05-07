using dotnet_server._Dtos;
using dotnet_server._Integrations;
using dotnet_server._Models;
using dotnet_server._Utils;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_server._Controllers;

[ApiController]
[Route("api/quo-messaging")]
public class QuoMessagingController(IQuoLeadMessagingClient quoLeadMessagingClient) : ControllerBase
{
    [HttpPost("test")]
    public async Task<IActionResult> SendTestMessage([FromBody] TestQuoLeadMessageRequest request, CancellationToken cancellationToken)
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

        await quoLeadMessagingClient.NotifyNewLeadAsync(consultation, cancellationToken);

        return Ok(new
        {
            message = "Test message request sent to QuoLeadMessagingClient.",
            consultation.Name,
            consultation.PhoneNumber,
            consultation.Timeline
        });
    }

    private static bool HasAtLeastTwoWords(string value)
    {
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length >= 2;
    }
}

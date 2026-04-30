using dotnet_server._Data;
using dotnet_server._Dtos;
using dotnet_server._Models;
using Microsoft.AspNetCore.Mvc;

namespace dotnet_server._Controllers;

[ApiController]
[Route("api/consultations")]
public class ConsultationsController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Consultation>> Create([FromBody] CreateConsultationRequest request)
    {
        if (!request.AgreedToTerms)
        {
            ModelState.AddModelError(nameof(request.AgreedToTerms), "Terms must be accepted.");
            return ValidationProblem(ModelState);
        }

        var consultation = new Consultation
        {
            Name = request.Name,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Style = request.Style,
            Placement = request.Placement,
            Size = request.Size,
            Budget = request.Budget,
            Description = request.Description,
            AgreedToTerms = request.AgreedToTerms,
            PreferredDays = request.PreferredDays
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(x => new ConsultationPreferredDay { Day = x.Trim() })
                .ToList()
        };

        dbContext.Consultations.Add(consultation);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(Create), new { id = consultation.Id }, consultation);
    }
}

using dotnet_server._Data;
using dotnet_server._Dtos;
using dotnet_server._Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server._Controllers;

[ApiController]
[Route("api/admin/consultations")]
public class AdminConsultationsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Consultation>>> GetAll(CancellationToken cancellationToken)
    {
        var consultations = await dbContext.Consultations
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(consultations);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<Consultation>> UpdateStatus(Guid id, [FromBody] UpdateConsultationStatusRequest request, CancellationToken cancellationToken)
    {
        var consultation = await dbContext.Consultations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (consultation is null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<ConsultationStatus>(request.Status, ignoreCase: true, out var parsedStatus))
        {
            return ValidationProblem($"Invalid status '{request.Status}'.");
        }

        consultation.Status = parsedStatus;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(consultation);
    }
}

using dotnet_server._Data;
using dotnet_server._Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server._Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/consultations")]
public class AdminConsultationsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var consultations = await dbContext.Consultations
            .AsNoTracking()
            .Include(x => x.PreferredDays)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(consultations);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var consultation = await dbContext.Consultations
            .AsNoTracking()
            .Include(x => x.PreferredDays)
            .FirstOrDefaultAsync(x => x.Id == id);

        return consultation is null ? NotFound() : Ok(consultation);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateConsultationStatusRequest request)
    {
        var consultation = await dbContext.Consultations.FirstOrDefaultAsync(x => x.Id == id);
        if (consultation is null)
        {
            return NotFound();
        }

        consultation.Status = request.Status;
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}

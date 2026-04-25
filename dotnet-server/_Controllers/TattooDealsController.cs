using dotnet_server._Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server._Controllers;

[ApiController]
[Route("api/tattoo-deals")]
public class TattooDealsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAvailable()
    {
        var deals = await dbContext.TattooDeals
            .AsNoTracking()
            .Where(x => x.IsAvailable)
            .OrderByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(deals);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var deal = await dbContext.TattooDeals.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsAvailable);
        return deal is null ? NotFound() : Ok(deal);
    }
}

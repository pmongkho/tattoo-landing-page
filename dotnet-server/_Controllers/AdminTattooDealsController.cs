using System.Security.Claims;
using dotnet_server._Data;
using dotnet_server._Dtos;
using dotnet_server._Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server._Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/tattoo-deals")]
public class AdminTattooDealsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var deals = await dbContext.TattooDeals
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(deals);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertTattooDealRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var deal = new TattooDeal
        {
            Title = request.Title,
            Style = request.Style,
            Placement = request.Placement,
            Size = request.Size,
            OriginalPrice = request.OriginalPrice,
            DiscountedPrice = request.DiscountedPrice,
            Description = request.Description,
            ReferenceImageUrl = request.ReferenceImageUrl,
            IsAvailable = request.IsAvailable,
            IsFeatured = request.IsFeatured,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.TattooDeals.Add(deal);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = deal.Id }, deal);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertTattooDealRequest request)
    {
        var deal = await dbContext.TattooDeals.FirstOrDefaultAsync(x => x.Id == id);
        if (deal is null)
        {
            return NotFound();
        }

        deal.Title = request.Title;
        deal.Style = request.Style;
        deal.Placement = request.Placement;
        deal.Size = request.Size;
        deal.OriginalPrice = request.OriginalPrice;
        deal.DiscountedPrice = request.DiscountedPrice;
        deal.Description = request.Description;
        deal.ReferenceImageUrl = request.ReferenceImageUrl;
        deal.IsAvailable = request.IsAvailable;
        deal.IsFeatured = request.IsFeatured;
        deal.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Disable(Guid id)
    {
        var deal = await dbContext.TattooDeals.FirstOrDefaultAsync(x => x.Id == id);
        if (deal is null)
        {
            return NotFound();
        }

        deal.IsAvailable = false;
        deal.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync();
        return NoContent();
    }
}

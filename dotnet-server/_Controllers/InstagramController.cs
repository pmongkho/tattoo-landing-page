using System.Security.Cryptography;
using System.Text;
using dotnet_server._Data;
using dotnet_server._Integrations;
using dotnet_server._Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace dotnet_server._Controllers;

[ApiController]
[Route("api/portfolio")]
public class InstagramController(AppDbContext db, IOptions<InstagramOptions> options, InstagramSyncService sync) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var items = await db.InstagramMedia.AsNoTracking()
        .Where(x => x.Visible).OrderByDescending(x => x.Pinned).ThenBy(x => x.DisplayOrder)
        .ThenByDescending(x => x.PublishedAt).ThenBy(x => x.Id)
        .Select(x => new { x.Id, imageUrl = x.ThumbnailUrl ?? x.MediaUrl, x.MediaUrl, x.MediaType,
            title = x.Caption == "" ? "Tattoo by Wo Hu" : x.Caption, x.Caption, x.Permalink, x.Pinned })
        .ToListAsync(ct);
        return Ok(new { enabled = !string.IsNullOrWhiteSpace(options.Value.AccessToken) || await db.InstagramMedia.AnyAsync(ct), items });
    }

    [HttpGet("manage")]
    public async Task<IActionResult> Manage(CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized();
        return Ok(new { configured = !string.IsNullOrWhiteSpace(options.Value.AccessToken), items = await db.InstagramMedia.AsNoTracking()
            .OrderByDescending(x => x.Pinned).ThenBy(x => x.DisplayOrder).ThenByDescending(x => x.PublishedAt).ThenBy(x => x.Id).ToListAsync(ct) });
    }

    [HttpPatch("manage/{id}")]
    public async Task<IActionResult> Update(string id, GallerySettings settings, CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized();
        var item = await db.InstagramMedia.FindAsync([id], ct);
        if (item is null) return NotFound();
        item.Visible = settings.Visible;
        item.Pinned = settings.Pinned;
        item.DisplayOrder = settings.DisplayOrder;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized();
        if (string.IsNullOrWhiteSpace(options.Value.AccessToken))
            return Problem("Set Instagram__AccessToken on the backend, then restart it.", statusCode: 503);
        try { return Ok(new { synced = await sync.SyncAsync(ct) }); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { return Problem("Instagram sync failed. Check the server token, permissions, API version and network access. Your saved gallery is unchanged.", statusCode: 502); }
    }

    private bool Authorized()
    {
        var expected = options.Value.AdminKey;
        var supplied = Request.Headers["X-Media-Admin-Key"].ToString();
        return expected.Length >= 32 && supplied.Length > 0 && CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(supplied)));
    }
    public record GallerySettings(bool Visible, bool Pinned, int DisplayOrder);
}

using dotnet_server._Data;
using dotnet_server._Integrations;
using Microsoft.EntityFrameworkCore;

namespace dotnet_server._Services;

public class InstagramSyncService(AppDbContext db, InstagramClient instagram)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<int> SyncAsync(CancellationToken ct)
    {
        await Gate.WaitAsync(ct);
        try
        {
            // Fetch the complete batch first so an upstream failure preserves the last successful gallery.
            var incoming = await instagram.FetchAsync(ct);
            var existing = await db.InstagramMedia.ToDictionaryAsync(x => x.Id, ct);
            foreach (var item in incoming.DistinctBy(x => x.Id))
            {
                if (!existing.TryGetValue(item.Id, out var saved))
                {
                    saved = item;
                    db.InstagramMedia.Add(saved);
                    existing.Add(item.Id, saved);
                }
                saved.MediaUrl = item.MediaUrl;
                saved.ThumbnailUrl = item.ThumbnailUrl;
                saved.Caption = item.Caption;
                saved.Permalink = item.Permalink;
                saved.MediaType = item.MediaType;
                saved.IsReel = item.IsReel;
                saved.PublishedAt = item.PublishedAt;
                saved.SyncedAt = DateTimeOffset.UtcNow;
                // Preserve website visibility, pinning, and order across syncs.
            }
            await db.SaveChangesAsync(ct);
            return incoming.Count;
        }
        finally { Gate.Release(); }
    }
}

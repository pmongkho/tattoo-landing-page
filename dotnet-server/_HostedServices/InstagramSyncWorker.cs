using dotnet_server._Integrations;
using dotnet_server._Services;
using Microsoft.Extensions.Options;

namespace dotnet_server._HostedServices;

public class InstagramSyncWorker(IServiceScopeFactory scopes, IOptions<InstagramOptions> options,
    ILogger<InstagramSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!string.IsNullOrWhiteSpace(options.Value.AccessToken))
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<InstagramSyncService>().SyncAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch { logger.LogWarning("Instagram sync failed. Last saved gallery retained; check token, permissions, API version and network access."); }
            }
            await Task.Delay(TimeSpan.FromMinutes(Math.Clamp(options.Value.SyncMinutes, 5, 1440)), stoppingToken);
        }
    }
}

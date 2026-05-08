using dotnet_server._Services;

namespace dotnet_server._HostedServices;

public class SquareDepositBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<SquareDepositBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ISquareDepositService>();
                await service.CheckAcceptedBookingsWithoutInvoicesAsync(stoppingToken);
                await service.MarkOverdueDepositsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Square deposit background run");
            }

            await Task.Delay(TimeSpan.FromMinutes(20), stoppingToken);
        }
    }
}

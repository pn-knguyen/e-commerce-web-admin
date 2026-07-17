using e_commerce_web_admin.Integrations.GiaoHangNhanh;
using Microsoft.Extensions.Options;

namespace e_commerce_web_admin.Services.Shipping;

public sealed class ShipmentStatusSyncWorker : BackgroundService
{
    private const int MinStatusSyncIntervalSeconds = 1;
    private const int MaxStatusSyncIntervalSeconds = 3600;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<GiaoHangNhanhOptions> _options;
    private readonly ILogger<ShipmentStatusSyncWorker> _logger;

    public ShipmentStatusSyncWorker(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<GiaoHangNhanhOptions> options,
        ILogger<ShipmentStatusSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            if (options.Enabled && options.EnableBackgroundStatusSync)
            {
                await SyncOnceAsync(stoppingToken);
            }

            await Task.Delay(GetInterval(options), stoppingToken);
        }
    }

    private async Task SyncOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var shipmentService = scope.ServiceProvider.GetRequiredService<IShipmentAdminService>();
            var syncedCount = await shipmentService.SyncActiveProviderStatusesAsync(ct);
            if (syncedCount > 0)
            {
                _logger.LogInformation("Synced {ShipmentCount} GHN shipment statuses.", syncedCount);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not sync GHN shipment statuses.");
        }
    }

    private static TimeSpan GetInterval(GiaoHangNhanhOptions options)
    {
        var seconds = Math.Clamp(
            options.StatusSyncIntervalSeconds,
            MinStatusSyncIntervalSeconds,
            MaxStatusSyncIntervalSeconds);
        return TimeSpan.FromSeconds(seconds);
    }
}

using System.Threading.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace e_commerce_web_admin.Services.CustomerMessages;

public interface ICustomerMessageRateLimiter
{
    Task<bool> TryAcquireCustomerSendAsync(long customerId, CancellationToken ct = default);
}

public sealed class CustomerMessageRateLimiter(IMemoryCache cache) : ICustomerMessageRateLimiter
{
    private static readonly TimeSpan CustomerWindow = TimeSpan.FromMinutes(1);
    private const int CustomerPermitLimit = 24;

    public Task<bool> TryAcquireCustomerSendAsync(long customerId, CancellationToken ct = default) =>
        TryAcquireAsync($"customer-message:send:{customerId}", CustomerPermitLimit, CustomerWindow, ct);

    private async Task<bool> TryAcquireAsync(
        string key,
        int permitLimit,
        TimeSpan window,
        CancellationToken ct)
    {
        var limiter = cache.GetOrCreate(key, entry =>
        {
            entry.SlidingExpiration = window.Add(window);
            entry.RegisterPostEvictionCallback(static (_, value, _, _) =>
            {
                (value as IDisposable)?.Dispose();
            });

            return new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = permitLimit,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                Window = window,
            });
        });

        if (limiter is null)
        {
            return false;
        }

        using var lease = await limiter.AcquireAsync(1, ct);
        return lease.IsAcquired;
    }
}

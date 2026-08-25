using System.Diagnostics;
using System.Net.Http;

namespace StorePos.Desktop.Startup;

public sealed class ApiReadinessService
{
    public static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromMilliseconds(750);
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(45);
    public const int DefaultMaximumAttempts = 60;

    private readonly HttpClient _httpClient;
    private readonly TimeSpan _retryInterval;
    private readonly TimeSpan _timeout;
    private readonly int _maximumAttempts;

    public ApiReadinessService(
        HttpClient httpClient,
        TimeSpan? retryInterval = null,
        TimeSpan? timeout = null,
        int maximumAttempts = DefaultMaximumAttempts)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _retryInterval = retryInterval ?? DefaultRetryInterval;
        _timeout = timeout ?? DefaultTimeout;
        ArgumentOutOfRangeException.ThrowIfLessThan(_retryInterval, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_timeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumAttempts);

        _httpClient = httpClient;
        _maximumAttempts = maximumAttempts;
    }

    public async Task<bool> WaitUntilReadyAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var timeoutCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(_timeout);

        for (var attempt = 1; attempt <= _maximumAttempts; attempt++)
        {
            try
            {
                using var response = await _httpClient.GetAsync(
                    "health",
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCancellation.Token);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                Trace.TraceWarning(
                    "StorePos API readiness attempt {0} returned HTTP {1}.",
                    attempt,
                    (int)response.StatusCode);
            }
            catch (HttpRequestException exception)
            {
                Trace.TraceWarning(
                    "StorePos API readiness attempt {0} failed: {1}",
                    attempt,
                    exception);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested &&
                      timeoutCancellation.IsCancellationRequested)
            {
                return false;
            }

            if (attempt == _maximumAttempts)
            {
                break;
            }

            try
            {
                await Task.Delay(_retryInterval, timeoutCancellation.Token);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested &&
                      timeoutCancellation.IsCancellationRequested)
            {
                return false;
            }
        }

        return false;
    }
}

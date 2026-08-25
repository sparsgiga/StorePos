using System.Net;
using StorePos.Desktop.Startup;

namespace StorePos.Desktop.Tests.Startup;

public sealed class ApiReadinessServiceTests
{
    [Fact]
    public async Task HealthSuccess_EndsWaiting()
    {
        using var client = CreateClient((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)));
        var service = CreateService(client);

        var result = await service.WaitUntilReadyAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task TemporaryConnectionFailure_RetriesUntilHealthy()
    {
        using var client = CreateClient((attempt, _) =>
            attempt == 1
                ? Task.FromException<HttpResponseMessage>(
                    new HttpRequestException("Connection refused."))
                : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var service = CreateService(client, maximumAttempts: 2);

        var result = await service.WaitUntilReadyAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task CallerCancellation_IsPropagated()
    {
        var requestStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = CreateClient(async (_, cancellationToken) =>
        {
            requestStarted.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = CreateService(client);
        using var cancellation = new CancellationTokenSource();

        var waitTask = service.WaitUntilReadyAsync(cancellation.Token);
        await requestStarted.Task;
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitTask);
    }

    [Fact]
    public async Task MaximumAttemptsReached_ReturnsFailure()
    {
        using var client = CreateClient((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var service = CreateService(client, maximumAttempts: 2);

        var result = await service.WaitUntilReadyAsync();

        Assert.False(result);
    }

    private static ApiReadinessService CreateService(
        HttpClient client,
        int maximumAttempts = 1)
        => new(
            client,
            retryInterval: TimeSpan.Zero,
            timeout: TimeSpan.FromSeconds(1),
            maximumAttempts);

    private static HttpClient CreateClient(
        Func<int, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        => new(new StubHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri("http://localhost/")
        };

    private sealed class StubHttpMessageHandler(
        Func<int, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        private int _attempt;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => responseFactory(Interlocked.Increment(ref _attempt), cancellationToken);
    }
}

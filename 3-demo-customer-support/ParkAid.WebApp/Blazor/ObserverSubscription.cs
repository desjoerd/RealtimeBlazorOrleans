using ParkAid.WebApp.Grains.Observers;

namespace ParkAid.WebApp.Blazor;

public static class ObserverExtensions
{
    public static IAsyncDisposable WatchGrain<TGrain, TObserver>(
        this IGrainFactory grainFactory,
        string grainId,
        TObserver observerInstance,
        ILoggerFactory loggerFactory)
        where TGrain : IGrainWithStringKey, IGrainObservable<TObserver>
        where TObserver : IGrainObserver
    {
        var grain = grainFactory.GetGrain<TGrain>(grainId);
        return grainFactory.WatchGrain(grain, observerInstance, loggerFactory);
    }

    public static IAsyncDisposable WatchGrain<TGrain, TObserver>(
        this IGrainFactory grainFactory,
        TGrain grain,
        TObserver observerInstance,
        ILoggerFactory loggerFactory)
        where TGrain : IGrainWithStringKey, IGrainObservable<TObserver>
        where TObserver : IGrainObserver
    {
        var observerObjectReference = grainFactory.CreateObjectReference<TObserver>(observerInstance);
        var result = new ObserverSubscription<TGrain, TObserver>(observerInstance, grain, observerObjectReference, loggerFactory.CreateLogger<ObserverSubscription<TGrain, TObserver>>());

        return result;
    }
}

public class ObserverSubscription<TObservableGrain, TObserver> : IAsyncDisposable
    where TObservableGrain : IGrain, IGrainObservable<TObserver>
    where TObserver : IGrainObserver
{
    private readonly CancellationTokenSource cancellation = new();
    private readonly WeakReference observerInstance;
    private readonly TObservableGrain observedGrain;
    private readonly TObserver observerObjectReference;
    private readonly Task watcherTask;
    private readonly ILogger logger;

    public ObserverSubscription(
        TObserver observerInstance,
        TObservableGrain observedGrain,
        TObserver observerObjectReference,
        ILogger logger)
    {
        this.observerInstance = new WeakReference(observerInstance);
        this.observedGrain = observedGrain;
        this.observerObjectReference = observerObjectReference;
        this.logger = logger;

        watcherTask = Task.Run(WatchObservable);
    }

    private async Task WatchObservable()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(270));

        logger.LogInformation("Starting watcher task");
        if (!observerInstance.IsAlive)
        {
            logger.LogInformation("Watching not started, the watcher has been GC'ed");
            return;
        }

        await observedGrain.Subscribe(observerObjectReference);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellation.Token))
            {
                // When the client disconnects, the .NET garbage collector can clean up the watcher object.
                // When that happens, we will stop watching.
                // Until then, periodically heartbeat the poll grain to let it know we're still watching.
                if (observerInstance.IsAlive)
                {
                    try
                    {
                        await observedGrain.Subscribe(observerObjectReference);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to refresh subscription");
                    }
                }
                else
                {
                    // The poll watcher object has been cleaned up, so stop refreshing its subscription.
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The watcher task has been cancelled, so stop refreshing its subscription.
        }

        // Notify the poll grain that we are no longer interested
        await observedGrain.Unsubscribe(observerObjectReference);
    }

    public async ValueTask DisposeAsync()
    {
        cancellation.Cancel();
        try
        {
            logger.LogInformation("Stopping watcher task");
            await watcherTask;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to stop watcher task");
        }

        cancellation.Dispose();
    }
}
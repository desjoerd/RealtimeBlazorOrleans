using Orleans.Concurrency;
using Orleans.Utilities;
using Orleans.Streams;

namespace MinimalBlazorOrleans.Grains;

public interface ICounterGrain
    : IGrainWithStringKey, IGrainObservable<ICounterObserver>
{
    Task Increment();
    Task Decrement();
    Task<int> GetCount();
}

public interface ICounterObserver : IGrainObserver
{
    [OneWay]
    Task OnCountUpdated(int count);
}

public class CounterGrain(ILogger<CounterGrain> logger) : Grain, ICounterGrain
{
    private readonly ObserverManager<ICounterObserver> observerManager = new ObserverManager<ICounterObserver>(expiration: TimeSpan.FromMinutes(5), logger);

    private int Count { get; set; }

    public async Task Increment()
    {
        Count++;
        await PublishUpdate();
    }

    public async Task Decrement()
    {
        Count--;
        await PublishUpdate();
    }

    public Task<int> GetCount()
    {
        return Task.FromResult(Count);
    }

    private async Task PublishUpdate()
    {
        await observerManager.Notify(o => o.OnCountUpdated(Count));

        await this.GetStreamProvider("DefaultStreaming")
            .GetStream<int>(nameof(ICounterGrain), this.GetPrimaryKeyString())
            .OnNextAsync(Count);
    }

    public Task Subscribe(ICounterObserver watcher)
    {
        observerManager.Subscribe(watcher, watcher);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(ICounterObserver watcher)
    {
        observerManager.Unsubscribe(watcher);
        return Task.CompletedTask;
    }
}
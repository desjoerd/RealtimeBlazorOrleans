namespace MinimalBlazorOrleans.Grains;

public interface IGrainObservable<in TObserver>
    where TObserver : IGrainObserver
{
    Task Subscribe(TObserver watcher);
    Task Unsubscribe(TObserver watcher);
}
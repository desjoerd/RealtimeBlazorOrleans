namespace ParkAid.WebApp.Grains.Observers;

public interface IGrainObservable<in TObserver>
    where TObserver : IGrainObserver
{
    Task Subscribe(TObserver watcher);
    Task Unsubscribe(TObserver watcher);
}
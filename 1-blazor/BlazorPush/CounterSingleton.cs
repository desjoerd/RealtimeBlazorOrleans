namespace BlazorPush;

public class CounterSingleton
{
    private int _count = 0;

    public int Count => _count;

    public void Increment()
    {
        Interlocked.Increment(ref _count);

        OnCountChanged();
    }

    public void Decrement()
    {
        Interlocked.Decrement(ref _count);

        OnCountChanged();
    }

    public event EventHandler<EventArgs>? CountChanged;

    private void OnCountChanged()
    {
        CountChanged?.Invoke(this, EventArgs.Empty);
    }
}
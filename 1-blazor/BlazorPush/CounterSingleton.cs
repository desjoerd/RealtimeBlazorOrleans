namespace BlazorPush;

public class CounterSingleton
{
    private int count = 0;

    public int Count => count;

    public void Increment()
    {
        Interlocked.Increment(ref count);

        OnCountChanged();
    }

    public void Decrement()
    {
        Interlocked.Decrement(ref count);

        OnCountChanged();
    }

    public event EventHandler<EventArgs>? CountChanged;

    private void OnCountChanged()
    {
        CountChanged?.Invoke(this, EventArgs.Empty);
    }
}
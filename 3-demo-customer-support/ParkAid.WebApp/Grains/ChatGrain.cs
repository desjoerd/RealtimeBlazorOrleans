using Orleans.Concurrency;
using Orleans.Providers;
using Orleans.Utilities;
using ParkAid.WebApp.Grains.Observers;

namespace ParkAid.WebApp.Grains;

public interface IChatGrain : IGrainWithStringKey, IGrainObservable<IChatObserver>
{
    Task SendMessage(string message, SenderType senderType, ICastMemberGrain? sender);
    Task<IList<ChatMessage>> GetMessages();

    Task ClaimChat(ICastMemberGrain castMember);
}

public interface IChatObserver : IGrainObserver
{
    [OneWay]
    Task OnMessageReceived(ChatMessage message);
}

public enum SenderType
{
    CastMember,
    Guest
}

[GenerateSerializer, Immutable]
public sealed class ChatMessage
{
    [Id(1000)]
    public int Sequence { get; init; }

    [Id(0)]
    public string? Message { get; init; }

    [Id(1)]
    public SenderType SenderType { get; init; }

    [Id(2)]
    public DateTime TimestampUtc { get; init; }

    [Id(3)]
    public ICastMemberGrain? Sender { get; init; }
}

public enum ChatStatus
{
    Open,
    Closed
}

public class ChatState
{
    [Id(0)]
    public IList<ChatMessage> Messages { get; } = new List<ChatMessage>();

    [Id(1)]
    public ChatStatus Status { get; set; } = ChatStatus.Open;

    [Id(2)]
    public ICastMemberGrain? CastMember { get; set; }
}

public class ChatGrain(
    ILogger<ChatGrain> logger,
    [PersistentState("chat", "Default")] IPersistentState<ChatState> state) : Grain, IChatGrain
{
    private readonly ObserverManager<IChatObserver> observerManager = new(TimeSpan.FromMinutes(5), logger);

    public async Task SendMessage(string message, SenderType senderType, ICastMemberGrain? sender)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be null or empty", nameof(message));
        }

        var chatMessage = new ChatMessage
        {
            Sequence = state.State.Messages.Count + 1,
            Message = message,
            SenderType = senderType,
            TimestampUtc = DateTime.UtcNow,
            Sender = sender,
        };
        state.State.Messages.Add(chatMessage);
        await state.WriteStateAsync();

        await observerManager.Notify(x => x.OnMessageReceived(chatMessage));
        await this.GetStreamProvider("DefaultStreaming")
            .GetStream<ChatMessage>(StreamId.Create(nameof(IChatGrain), this.GetPrimaryKeyString()))
            .OnNextAsync(chatMessage);
    }

    public Task<IList<ChatMessage>> GetMessages()
    {
        return Task.FromResult(state.State.Messages);
    }

    public async Task ClaimChat(ICastMemberGrain castMember)
    {
        if (state.State.Status == ChatStatus.Closed)
        {
            throw new InvalidOperationException("Chat is closed");
        }

        state.State.Status = ChatStatus.Open;
        state.State.CastMember = castMember;

        await state.WriteStateAsync();
    }

    public Task Subscribe(IChatObserver watcher)
    {
        observerManager.Subscribe(watcher, watcher);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IChatObserver watcher)
    {
        observerManager.Unsubscribe(watcher);
        return Task.CompletedTask;
    }
}
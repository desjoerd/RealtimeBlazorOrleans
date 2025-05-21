using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Streams;

namespace ParkAid.WebApp.Grains;

public interface IChatQueueGrain : IGrainWithStringKey
{
  Task<IChatGrain> RequestChat();
  Task<IChatGrain?> GetNextUnclaimedChat();
  Task<bool> ClaimChat(string chatId, string castMemberId);
  Task<bool> HasUnclaimedChat();
  Task<bool> ClaimNextChat(string castMemberId);
}

public class ChatQueueState
{
  public List<string> UnclaimedChats { get; set; } = new();
  public Dictionary<string, string> ClaimedChats { get; set; } = new(); // chatId -> castMemberId
}

[Reentrant]
public class ChatQueueGrain : Grain, IChatQueueGrain, IAsyncObserver<string>
{
  private readonly IPersistentState<ChatQueueState> _state;

  public ChatQueueGrain(
      [PersistentState("chatqueue", "Default")] IPersistentState<ChatQueueState> state)
  {
    _state = state;
  }

  public override async Task OnActivateAsync(CancellationToken cancellationToken)
  {
      var stream = this.GetStreamProvider("DefaultStreaming").GetStream<string>(StreamId.Create("chatqueue", this.GetPrimaryKeyString()));

      var handles = await stream.GetAllSubscriptionHandles();
      if (handles.Count == 0)
      {
        await stream.SubscribeAsync(this);
      }
      else
      {
        await handles[0].ResumeAsync(this);
        foreach(var handle in handles.Skip(1))
        {
           await handle.UnsubscribeAsync();
        }
      }
  }

  public async Task<IChatGrain> RequestChat()
  {
    var chatId = Guid.NewGuid().ToString();
    var chatGrain = GrainFactory.GetGrain<IChatGrain>(chatId);

    _state.State.UnclaimedChats.Add(chatId);
    await _state.WriteStateAsync();

    return chatGrain;
  }

  public Task<IChatGrain?> GetNextUnclaimedChat()
  {
    if (!_state.State.UnclaimedChats.Any())
    {
      return Task.FromResult<IChatGrain?>(null);
    }

    var nextChatId = _state.State.UnclaimedChats.FirstOrDefault();
    if (string.IsNullOrEmpty(nextChatId))
    {
      return Task.FromResult<IChatGrain?>(null);
    }

    var chatGrain = GrainFactory.GetGrain<IChatGrain>(nextChatId);
    return Task.FromResult<IChatGrain?>(chatGrain);
  }

  public async Task<bool> ClaimChat(string chatId, string castMemberId)
  {
    if (!_state.State.UnclaimedChats.Contains(chatId))
    {
      return false;
    }

    var castMemberGrain = GrainFactory.GetGrain<ICastMemberGrain>(castMemberId);
    var success = await castMemberGrain.ClaimChat(chatId);

    if (success)
    {
      _state.State.UnclaimedChats.Remove(chatId);
      _state.State.ClaimedChats[chatId] = castMemberId;
      await _state.WriteStateAsync();
      return true;
    }

    return false;
  }

  public Task<bool> HasUnclaimedChat()
  {
    return Task.FromResult(_state.State.UnclaimedChats.Any());
  }

  public async Task<bool> ClaimNextChat(string castMemberId)
  {
    if (!_state.State.UnclaimedChats.Any())
    {
      return false;
    }

    var chatId = _state.State.UnclaimedChats.First();
    return await ClaimChat(chatId, castMemberId);
  }

  public Task OnNextAsync(string item, StreamSequenceToken? token = null)
  {
    throw new NotImplementedException();
  }

  public Task OnErrorAsync(Exception ex)
  {
    throw new NotImplementedException();
  }
}

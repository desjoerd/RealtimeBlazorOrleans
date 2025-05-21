using Orleans.Runtime;

namespace ParkAid.WebApp.Grains;

public interface ICastMemberGrain : IGrainWithStringKey
{
  Task<string> GetName();
  Task<IList<string>> GetAssignedChats();
  Task AddAssignedChat(string chatId);
  Task RemoveAssignedChat(string chatId);
  Task<bool> ClaimChat(string chatId);
}

public class CastMemberState
{
  public string Name { get; set; } = string.Empty;
  public List<string> AssignedChats { get; set; } = new();
}

public class CastMemberGrain : Grain, ICastMemberGrain
{
  private readonly IPersistentState<CastMemberState> _state;

  public CastMemberGrain(
      [PersistentState("castmember", "Default")] IPersistentState<CastMemberState> state)
  {
    _state = state;
  }

  public override async Task OnActivateAsync(CancellationToken cancellationToken)
  {
    // Set name if not already set
    if (string.IsNullOrEmpty(_state.State.Name))
    {
      _state.State.Name = this.GetPrimaryKeyString();
      await _state.WriteStateAsync();
    }

    await base.OnActivateAsync(cancellationToken);
  }

  public Task<string> GetName() => Task.FromResult(_state.State.Name);

  public Task<IList<string>> GetAssignedChats() => Task.FromResult<IList<string>>(_state.State.AssignedChats);

  public async Task AddAssignedChat(string chatId)
  {
    if (!_state.State.AssignedChats.Contains(chatId))
    {
      _state.State.AssignedChats.Add(chatId);
      await _state.WriteStateAsync();
    }
  }

  public async Task RemoveAssignedChat(string chatId)
  {
    if (_state.State.AssignedChats.Contains(chatId))
    {
      _state.State.AssignedChats.Remove(chatId);
      await _state.WriteStateAsync();
    }
  }

  public async Task<bool> ClaimChat(string chatId)
  {
    var chatGrain = GrainFactory.GetGrain<IChatGrain>(chatId);
    await chatGrain.ClaimChat(this.AsReference<ICastMemberGrain>());

    await AddAssignedChat(chatId);
    return true;
  }
}

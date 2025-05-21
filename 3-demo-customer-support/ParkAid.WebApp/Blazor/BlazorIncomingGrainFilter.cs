using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace ParkAid.WebApp.Blazor;

public class BlazorIncomingGrainFilter : IIncomingGrainCallFilter
{
    private readonly MethodInfo invokeAsyncMethod = typeof(ComponentBase)
        .GetMethod("InvokeAsync",
            BindingFlags.Instance | BindingFlags.NonPublic,
            [typeof(Func<Task>)])!;

    private readonly MethodInfo stateHasChangedMethod = typeof(ComponentBase)
        .GetMethod("StateHasChanged",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    public Task Invoke(IIncomingGrainCallContext context)
    {
        if (context.Grain is ComponentBase componentBase)
        {
            return InvokeComponent(componentBase, context.Invoke);
        }

        return context.Invoke();
    }

    private async Task InvokeComponent(ComponentBase componentBase, Func<Task> invokeNotification)
    {
        async Task ExecuteNotificationOnComponent()
        {
            await invokeNotification();
            stateHasChangedMethod.Invoke(componentBase, null);
        }

        await (Task)invokeAsyncMethod.Invoke(
            componentBase,
            [(Func<Task>)ExecuteNotificationOnComponent])!;
    }
}

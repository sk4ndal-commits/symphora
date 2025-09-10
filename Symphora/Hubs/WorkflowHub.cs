using Microsoft.AspNetCore.SignalR;

namespace Symphora.Hubs;

public class WorkflowHub : Hub
{
    public async Task JoinWorkflowGroup(string workflowId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workflow-{workflowId}");
    }

    public async Task LeaveWorkflowGroup(string workflowId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workflow-{workflowId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
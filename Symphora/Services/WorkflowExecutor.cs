using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Symphora.Data;
using Symphora.Hubs;
using Symphora.Models;
using System.Text.Json;

namespace Symphora.Services;

public interface IWorkflowExecutor
{
    Task<bool> ExecuteWorkflowAsync(Guid workflowId, Dictionary<string, object>? parameters = null);
    Task<bool> CancelWorkflowExecutionAsync(Guid workflowId);
    Task<List<ExecutionLog>> GetWorkflowExecutionLogsAsync(Guid workflowId);
}

public class WorkflowExecutor : IWorkflowExecutor
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHubContext<WorkflowHub> _hubContext;
    private readonly ILogger<WorkflowExecutor> _logger;
    private readonly Dictionary<Guid, CancellationTokenSource> _runningWorkflows;

    public WorkflowExecutor(
        IServiceScopeFactory serviceScopeFactory, 
        IHubContext<WorkflowHub> hubContext,
        ILogger<WorkflowExecutor> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _hubContext = hubContext;
        _logger = logger;
        _runningWorkflows = new Dictionary<Guid, CancellationTokenSource>();
    }

    public async Task<bool> ExecuteWorkflowAsync(Guid workflowId, Dictionary<string, object>? parameters = null)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var workflow = await context.Workflows
                .Include(w => w.Owner)
                .FirstOrDefaultAsync(w => w.Id == workflowId);

            if (workflow == null)
            {
                _logger.LogError("Workflow {WorkflowId} not found", workflowId);
                return false;
            }

            // Check if workflow is already running
            if (_runningWorkflows.ContainsKey(workflowId))
            {
                _logger.LogWarning("Workflow {WorkflowId} is already running", workflowId);
                return false;
            }

            var cancellationTokenSource = new CancellationTokenSource();
            _runningWorkflows[workflowId] = cancellationTokenSource;

            // Start execution in background
            _ = Task.Run(async () => await ExecuteWorkflowInternalAsync(workflow, parameters, cancellationTokenSource.Token));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting workflow execution for {WorkflowId}", workflowId);
            return false;
        }
    }

    public async Task<bool> CancelWorkflowExecutionAsync(Guid workflowId)
    {
        if (_runningWorkflows.TryGetValue(workflowId, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
            _runningWorkflows.Remove(workflowId);

            // Log cancellation
            await LogExecutionStepAsync(workflowId, Guid.Empty, "Cancelled", "Workflow execution cancelled by user", null);
            await NotifyClientsAsync(workflowId, "Workflow execution cancelled");

            return true;
        }

        return false;
    }

    public async Task<List<ExecutionLog>> GetWorkflowExecutionLogsAsync(Guid workflowId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        return await context.ExecutionLogs
            .Where(log => log.WorkflowId == workflowId)
            .Include(log => log.Agent)
            .OrderBy(log => log.Timestamp)
            .ToListAsync();
    }

    private async Task ExecuteWorkflowInternalAsync(Workflow workflow, Dictionary<string, object>? parameters, CancellationToken cancellationToken)
    {
        try
        {
            await NotifyClientsAsync(workflow.Id, "Starting workflow execution...");

            // Parse workflow nodes and edges
            var nodes = JsonSerializer.Deserialize<List<WorkflowNode>>(workflow.NodesJson) ?? new List<WorkflowNode>();
            var edges = JsonSerializer.Deserialize<List<WorkflowEdge>>(workflow.EdgesJson) ?? new List<WorkflowEdge>();

            if (!nodes.Any())
            {
                await LogExecutionStepAsync(workflow.Id, Guid.Empty, "Failed", "No nodes found in workflow", "Workflow contains no executable nodes");
                await NotifyClientsAsync(workflow.Id, "Workflow execution failed: No nodes found");
                return;
            }

            // Find start node (assuming first node or node with no incoming edges)
            var startNode = nodes.FirstOrDefault(n => !edges.Any(e => e.Target == n.Id)) ?? nodes.First();

            // Execute workflow sequentially for now (can be made async later)
            await ExecuteNodeAsync(workflow.Id, startNode, nodes, edges, parameters, cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                await NotifyClientsAsync(workflow.Id, "Workflow execution completed successfully");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Workflow {WorkflowId} execution was cancelled", workflow.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow {WorkflowId}", workflow.Id);
            await LogExecutionStepAsync(workflow.Id, Guid.Empty, "Failed", "Workflow execution failed", ex.Message);
            await NotifyClientsAsync(workflow.Id, $"Workflow execution failed: {ex.Message}");
        }
        finally
        {
            _runningWorkflows.Remove(workflow.Id);
        }
    }

    private async Task ExecuteNodeAsync(Guid workflowId, WorkflowNode node, List<WorkflowNode> allNodes, List<WorkflowEdge> edges, Dictionary<string, object>? parameters, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Validate and parse AgentId
            if (string.IsNullOrWhiteSpace(node.Data.AgentId) || !Guid.TryParse(node.Data.AgentId, out var agentId))
            {
                await LogExecutionStepAsync(workflowId, Guid.Empty, "Failed", $"Invalid or missing agent ID for node {node.Id}", null);
                return;
            }

            // Get agent for this node using a scoped context
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var agent = await context.Agents.FindAsync(agentId);
            if (agent == null)
            {
                await LogExecutionStepAsync(workflowId, Guid.Empty, "Failed", $"Agent not found for node {node.Id}", null);
                return;
            }

            await LogExecutionStepAsync(workflowId, agent.Id, "Running", $"Executing agent: {agent.Name}", null);
            await NotifyClientsAsync(workflowId, $"Executing agent: {agent.Name}");

            // Simulate agent execution (replace with actual agent execution logic)
            var executionResult = await SimulateAgentExecutionAsync(agent, parameters, cancellationToken);

            await LogExecutionStepAsync(workflowId, agent.Id, executionResult.Success ? "Completed" : "Failed", executionResult.Output, executionResult.ErrorMessage);

            if (executionResult.Success)
            {
                await NotifyClientsAsync(workflowId, $"Agent {agent.Name} completed successfully");

                // Find next nodes to execute
                var nextEdges = edges.Where(e => e.Source == node.Id).ToList();
                foreach (var edge in nextEdges)
                {
                    var nextNode = allNodes.FirstOrDefault(n => n.Id == edge.Target);
                    if (nextNode != null)
                    {
                        await ExecuteNodeAsync(workflowId, nextNode, allNodes, edges, parameters, cancellationToken);
                    }
                }
            }
            else
            {
                await NotifyClientsAsync(workflowId, $"Agent {agent.Name} failed: {executionResult.ErrorMessage}");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing node {NodeId} in workflow {WorkflowId}", node.Id, workflowId);
            await LogExecutionStepAsync(workflowId, Guid.Empty, "Failed", $"Error executing node {node.Id}", ex.Message);
        }
    }

    private async Task<AgentExecutionResult> SimulateAgentExecutionAsync(Agent agent, Dictionary<string, object>? parameters, CancellationToken cancellationToken)
    {
        // Simulate agent execution time
        await Task.Delay(Random.Shared.Next(1000, 3000), cancellationToken);

        // Simple simulation based on agent type
        return agent.Type.ToLower() switch
        {
            "api" => new AgentExecutionResult { Success = true, Output = $"API call completed successfully for {agent.Name}" },
            "data" => new AgentExecutionResult { Success = true, Output = $"Data processing completed for {agent.Name}" },
            "ai" => new AgentExecutionResult { Success = true, Output = $"AI agent {agent.Name} completed analysis" },
            _ => new AgentExecutionResult { Success = true, Output = $"Custom agent {agent.Name} executed successfully" }
        };
    }

    private async Task LogExecutionStepAsync(Guid workflowId, Guid agentId, string status, string output, string? errorMessage)
    {
        var log = new ExecutionLog
        {
            WorkflowId = workflowId,
            AgentId = agentId == Guid.Empty ? Guid.NewGuid() : agentId, // Use placeholder for workflow-level logs
            Status = status,
            Output = output,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        context.ExecutionLogs.Add(log);
        await context.SaveChangesAsync();

        // Notify clients about the new log
        await _hubContext.Clients.Group($"workflow-{workflowId}").SendAsync("LogUpdate", log);
    }

    private async Task NotifyClientsAsync(Guid workflowId, string message)
    {
        await _hubContext.Clients.Group($"workflow-{workflowId}").SendAsync("StatusUpdate", message);
    }
}

// Helper classes for workflow execution
public class WorkflowNode
{
    public string Id { get; set; } = string.Empty;
    public WorkflowNodeData Data { get; set; } = new();
    public Position Position { get; set; } = new();
}

public class WorkflowNodeData
{
    public string AgentId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class Position
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class WorkflowEdge
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
}

public class AgentExecutionResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symphora.Services;

namespace Symphora.Controllers;

[Authorize]
public class WorkflowExecutionController : Controller
{
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly ILogger<WorkflowExecutionController> _logger;

    public WorkflowExecutionController(
        IWorkflowExecutor workflowExecutor,
        ILogger<WorkflowExecutionController> logger)
    {
        _workflowExecutor = workflowExecutor;
        _logger = logger;
    }

    // GET: WorkflowExecution/Dashboard/{workflowId}
    public async Task<IActionResult> Dashboard(Guid workflowId)
    {
        var logs = await _workflowExecutor.GetWorkflowExecutionLogsAsync(workflowId);
        
        ViewData["WorkflowId"] = workflowId;
        return View(logs);
    }

    // POST: /api/workflows/{workflowId}/execute
    [HttpPost]
    [Route("api/workflows/{workflowId:guid}/execute")]
    public async Task<IActionResult> ExecuteWorkflow(Guid workflowId, [FromBody] Dictionary<string, object>? parameters = null)
    {
        try
        {
            var success = await _workflowExecutor.ExecuteWorkflowAsync(workflowId, parameters);
            
            if (success)
            {
                return Json(new { success = true, message = "Workflow execution started successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Failed to start workflow execution" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow {WorkflowId}", workflowId);
            return Json(new { success = false, message = "An error occurred while starting workflow execution" });
        }
    }

    // POST: /api/workflows/{workflowId}/cancel
    [HttpPost]
    [Route("api/workflows/{workflowId:guid}/cancel")]
    public async Task<IActionResult> CancelWorkflow(Guid workflowId)
    {
        try
        {
            var success = await _workflowExecutor.CancelWorkflowExecutionAsync(workflowId);
            
            if (success)
            {
                return Json(new { success = true, message = "Workflow execution cancelled successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Workflow is not currently running or could not be cancelled" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling workflow {WorkflowId}", workflowId);
            return Json(new { success = false, message = "An error occurred while cancelling workflow execution" });
        }
    }

    // GET: /api/workflows/{workflowId}/logs
    [HttpGet]
    [Route("api/workflows/{workflowId:guid}/logs")]
    public async Task<IActionResult> GetLogs(Guid workflowId)
    {
        try
        {
            var logs = await _workflowExecutor.GetWorkflowExecutionLogsAsync(workflowId);
            return Json(logs.Select(log => new
            {
                id = log.Id,
                workflowId = log.WorkflowId,
                agentId = log.AgentId,
                agentName = log.Agent?.Name ?? "Unknown",
                status = log.Status,
                timestamp = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                output = log.Output,
                errorMessage = log.ErrorMessage
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs for workflow {WorkflowId}", workflowId);
            return Json(new { success = false, message = "An error occurred while retrieving workflow logs" });
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Symphora.Data;
using Symphora.Models;
using System.Security.Claims;

namespace Symphora.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public WorkflowController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: api/Workflow
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Workflow>>> GetWorkflows()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var workflows = await _context.Workflows
            .Where(w => w.OwnerId == userId)
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync();

        return Ok(workflows);
    }

    // GET: api/Workflow/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Workflow>> GetWorkflow(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var workflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == id);

        if (workflow == null)
        {
            return NotFound();
        }

        // Validate ownership
        if (workflow.OwnerId != userId)
        {
            return Forbid();
        }

        return Ok(workflow);
    }

    // POST: api/Workflow
    [HttpPost]
    public async Task<ActionResult<Workflow>> CreateWorkflow(CreateWorkflowDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        // Validate JSON structure
        if (!IsValidJson(dto.NodesJson) || !IsValidJson(dto.EdgesJson))
        {
            return BadRequest("Invalid JSON structure for nodes or edges.");
        }

        var workflow = new Workflow
        {
            Name = dto.Name,
            Description = dto.Description,
            NodesJson = dto.NodesJson ?? "[]",
            EdgesJson = dto.EdgesJson ?? "[]",
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWorkflow), new { id = workflow.Id }, workflow);
    }

    // PUT: api/Workflow/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkflow(Guid id, UpdateWorkflowDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var workflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == id);

        if (workflow == null)
        {
            return NotFound();
        }

        // Validate ownership
        if (workflow.OwnerId != userId)
        {
            return Forbid();
        }

        // Validate JSON structure
        if (!IsValidJson(dto.NodesJson) || !IsValidJson(dto.EdgesJson))
        {
            return BadRequest("Invalid JSON structure for nodes or edges.");
        }

        workflow.Name = dto.Name;
        workflow.Description = dto.Description;
        workflow.NodesJson = dto.NodesJson ?? "[]";
        workflow.EdgesJson = dto.EdgesJson ?? "[]";
        workflow.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(workflow);
    }

    // DELETE: api/Workflow/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkflow(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var workflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == id);

        if (workflow == null)
        {
            return NotFound();
        }

        // Validate ownership
        if (workflow.OwnerId != userId)
        {
            return Forbid();
        }

        _context.Workflows.Remove(workflow);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static bool IsValidJson(string? jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return true; // Empty or null is considered valid, will default to "[]"
        }

        try
        {
            JsonSerializer.Deserialize<object>(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

// DTOs for API requests
public class CreateWorkflowDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? NodesJson { get; set; }
    public string? EdgesJson { get; set; }
}

public class UpdateWorkflowDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? NodesJson { get; set; }
    public string? EdgesJson { get; set; }
}
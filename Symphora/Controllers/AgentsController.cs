using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Symphora.Data;
using Symphora.Models;
using System.Text.Json;

namespace Symphora.Controllers;

[Authorize]
public class AgentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AgentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Agents
    public async Task<IActionResult> Index()
    {
        var agents = await _context.Agents
            .OrderBy(a => a.Name)
            .ToListAsync();

        return View(agents);
    }

    // GET: Agents/{id}
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var agent = await _context.Agents.FirstOrDefaultAsync(a => a.Id == id);

        if (agent == null)
        {
            return NotFound();
        }

        return Json(agent);
    }

    // POST: Agents/{id}/Configure
    [HttpPost("Configure/{id}")]
    public async Task<IActionResult> Configure(Guid id, [FromBody] JsonElement parametersJson)
    {
        var agent = await _context.Agents.FirstOrDefaultAsync(a => a.Id == id);

        if (agent == null)
        {
            return NotFound();
        }

        try
        {
            // Validate that the JSON is well-formed
            var parametersString = parametersJson.GetRawText();
            JsonDocument.Parse(parametersString); // This will throw if invalid JSON

            agent.ParametersJson = parametersString;
            agent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Agent configuration saved successfully." });
        }
        catch (JsonException)
        {
            return BadRequest(new { success = false, message = "Invalid JSON format." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while saving configuration." });
        }
    }
}
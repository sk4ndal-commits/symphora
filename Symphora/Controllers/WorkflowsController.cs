using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Symphora.Data;
using Symphora.Models;
using System.Security.Claims;

namespace Symphora.Controllers;

[Authorize]
public class WorkflowsController : Controller
{
    private readonly ApplicationDbContext _context;

    public WorkflowsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Workflows
    public async Task<IActionResult> Index()
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

        return View(workflows);
    }

    // GET: Workflows/Create
    public IActionResult Create()
    {
        return View();
    }

    // GET: Workflows/Edit/5
    public async Task<IActionResult> Edit(Guid id)
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

        return View(workflow);
    }

    // GET: Workflows/Builder
    public IActionResult Builder(Guid? id)
    {
        ViewBag.WorkflowId = id;
        return View();
    }
}
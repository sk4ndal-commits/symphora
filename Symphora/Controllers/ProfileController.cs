using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Symphora.Models;
using Symphora.Models.ViewModels;

namespace Symphora.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProfileController> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        ILogger<ProfileController> logger,
        IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var model = new ProfileViewModel
        {
            Name = user.Name,
            Timezone = user.Timezone,
            CurrentAvatarUrl = user.AvatarUrl
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        // Update basic profile fields
        user.Name = model.Name;
        user.Timezone = model.Timezone;
        user.UpdatedAt = DateTime.UtcNow;

        // Handle avatar upload
        if (model.AvatarFile != null)
        {
            // Validate file type and size
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            var maxSizeBytes = 5 * 1024 * 1024; // 5MB

            if (!allowedTypes.Contains(model.AvatarFile.ContentType.ToLower()))
            {
                ModelState.AddModelError("AvatarFile", "Only JPEG, PNG and GIF images are allowed.");
                return View(model);
            }

            if (model.AvatarFile.Length > maxSizeBytes)
            {
                ModelState.AddModelError("AvatarFile", "Avatar file size must be less than 5MB.");
                return View(model);
            }

            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsPath);

                // Delete old avatar if exists
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var oldAvatarPath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldAvatarPath))
                    {
                        System.IO.File.Delete(oldAvatarPath);
                    }
                }

                // Save new avatar
                var fileName = $"{user.Id}_{DateTime.UtcNow.Ticks}{Path.GetExtension(model.AvatarFile.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);
                    
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(stream);
                }

                user.AvatarUrl = $"/uploads/avatars/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for user {UserId}", user.Id);
                ModelState.AddModelError("AvatarFile", "Error uploading avatar. Please try again.");
                return View(model);
            }
        }

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} updated their profile", user.Id);
            model.Message = "Profile updated successfully!";
            model.IsSuccess = true;
            model.CurrentAvatarUrl = user.AvatarUrl;
        }
        else
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.Message = "Error updating profile. Please try again.";
            model.IsSuccess = false;
        }

        return View(model);
    }

    // API endpoint for GET /Profile
    [HttpGet("api/profile")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Json(new
        {
            name = user.Name,
            email = user.Email,
            timezone = user.Timezone,
            avatarUrl = user.AvatarUrl,
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt
        });
    }

    // API endpoint for POST /Profile
    [HttpPost("api/profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([FromForm] ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Invalid data", errors = ModelState });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Use the same logic as the regular POST action
        // This would be refactored to a service in a real application
        user.Name = model.Name;
        user.Timezone = model.Timezone;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            return Json(new { message = "Profile updated successfully", success = true });
        }

        return BadRequest(new { message = "Error updating profile", errors = result.Errors });
    }
}
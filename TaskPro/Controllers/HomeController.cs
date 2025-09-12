using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using TaskPro.Helper;
using TaskPro.Models;

namespace TaskPro.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TaskProDbContext _context;

    public HomeController(ILogger<HomeController> logger, SignInManager<ApplicationUser> signInManager, TaskProDbContext context, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _signInManager = signInManager;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToPage("/Account/Login", new { returnUrl = Url.Action("Index", "Home") });
        }

        return View();
    }
  
    public IActionResult InternalServerError()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/Identity/Account/Login");
    }

    [HttpGet]
    public async Task<IActionResult> GetAllNotifications()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var allNotifications = await _context.Notifications
                                        .Where(n => n.UserId == userId)
                                        .OrderByDescending(n => n.CreatedAt)
                                        .ToListAsync();
        return Json(allNotifications.Select(n => new
        {
            id = n.Id,
            message = n.Message,
            isRead = n.IsRead ?? false,
            createdAt = n.CreatedAt
        }));
    }
}

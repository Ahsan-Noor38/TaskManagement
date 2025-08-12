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

    public IActionResult Index()
    {
        if (!User.Identity.IsAuthenticated)
        { 
            return RedirectToPage("/Account/Login", new { returnUrl = Url.Action("Index", "Home") });
        }
        else if (User.IsInRole("Member"))
        {
            return RedirectToAction("Index", "Member");
        }
        var now = DateTime.Now;
        var taskUpdates = _context.Tasks
                                    .SelectMany(t => t.TaskAssignments)
                                    .SelectMany(ta => ta.TaskUpdates)
                                    .Include(ta => ta.TaskAssignment.Task)
                                    .ToList();

        var taskOverdue = _context.Tasks.Count(t => t.Deadline < now && t.TaskAssignments.All(ta => ta.TaskUpdates.All(s => s.Status != (int)Models.Enum.TaskStatus.Completed)));
        ViewBag.PendingCount = taskUpdates.Count(tu => tu.Status == (int)Models.Enum.TaskStatus.Pending);
        ViewBag.InProgressCount = taskUpdates.Count(tu => tu.Status == (int)Models.Enum.TaskStatus.InProgress);
        ViewBag.CompletedCount = taskUpdates.Count(tu => tu.Status == (int)Models.Enum.TaskStatus.Completed);
        ViewBag.OverdueCount = taskOverdue;

        // New: Tasks per User
        var tasksPerUser = _context.TaskAssignments
            .Include(ta => ta.AssignedToNavigation) // navigation property to User
            .GroupBy(ta => ta.AssignedToNavigation.FullName)
            .Select(g => new
            {
                UserName = g.Key,
                TaskCount = g.Count()
            })
            .ToList();

        ViewBag.TasksPerUserLabels = tasksPerUser.Select(x => x.UserName).ToArray();
        ViewBag.TasksPerUserCounts = tasksPerUser.Select(x => x.TaskCount).ToArray();

        var roleCounts = new Dictionary<string, int>
                        {
                            {StaticDetails.Roles.Admin, _userManager.GetUsersInRoleAsync(StaticDetails.Roles.Admin).Result.Count },
                            { StaticDetails.Roles.Manager, _userManager.GetUsersInRoleAsync(StaticDetails.Roles.Manager).Result.Count },
                            {StaticDetails.Roles.Member, _userManager.GetUsersInRoleAsync(StaticDetails.Roles.Member).Result.Count }
                        };

        ViewBag.RoleLabels = roleCounts.Keys.ToList();
        ViewBag.RoleCounts = roleCounts.Values.ToList();

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
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using TaskPro.Helper;
using TaskPro.Models;

namespace TaskPro.Controllers;

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
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToPage("/Account/Login", new { returnUrl = Url.Action("Index", "Home") });
        }
        else if (User.IsInRole("Member"))
        {
            return RedirectToAction("Index", "Member");
        }

        bool isAdmin = User.IsInRole(StaticDetails.Roles.Admin);
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Base query with includes
        var taskUpdatesQuery = _context.TaskUpdates
            .Include(u => u.TaskAssignment)
                .ThenInclude(a => a.Task)
            .AsQueryable();

        IQueryable<Models.Task> taskQuery = _context.Tasks
            .Include(t => t.TaskAssignments)
                .ThenInclude(ta => ta.TaskUpdates)
            .AsQueryable();

        if (isAdmin)
        {
            // Admin sees all tasks created by managers or admins
            var managerIds = (await _userManager.GetUsersInRoleAsync(StaticDetails.Roles.Manager))
                                .Where(u => u.CreatedBy == userId)
                                .Select(u => u.Id)
                                .ToList();

            taskUpdatesQuery = taskUpdatesQuery.Where(u => managerIds.Contains(u.TaskAssignment.Task.CreatedBy) || u.TaskAssignment.Task.CreatedBy == userId);
            taskQuery = taskQuery.Where(t => managerIds.Contains(t.CreatedBy) || t.CreatedBy == userId);
        }
        else
        {
            // Manager sees only their own tasks
            taskUpdatesQuery = taskUpdatesQuery.Where(u => u.TaskAssignment.Task.CreatedBy == userId);
            taskQuery = taskQuery.Where(t => t.CreatedBy == userId);
        }

        // --- Execute queries ---
        var taskUpdates = await taskUpdatesQuery.ToListAsync();
        var now = DateTime.UtcNow;

        // Overdue count
        var taskOverdue = await taskQuery.CountAsync(t =>
            t.Deadline < now &&
            t.TaskAssignments.All(ta => ta.TaskUpdates.All(s => s.Status != (int)Models.Enum.TaskStatus.Completed)));

        // Status counts
        // Group by Task, then filter by "all updates are Pending"
        ViewBag.PendingCount = taskUpdates
            .GroupBy(tu => tu.TaskAssignment.Task.Id) // group by Task
            .Count(g => g.All(tu => tu.Status == (int)Models.Enum.TaskStatus.Pending));

        ViewBag.InProgressCount = taskUpdates
            .GroupBy(tu => tu.TaskAssignment.Task.Id)
            .Count(g => g.Any(tu => tu.Status == (int)Models.Enum.TaskStatus.InProgress));

        ViewBag.CompletedCount = taskUpdates
            .GroupBy(tu => tu.TaskAssignment.Task.Id)
            .Count(g => g.All(tu => tu.Status == (int)Models.Enum.TaskStatus.Completed));

        ViewBag.OverdueCount = taskOverdue;

        //User Per Task Counts
        var createdById = isAdmin ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value : User.FindFirst(ClaimTypes.PrimarySid)?.Value;
        var (labels, counts, userIds) = await GetTasksPerUserAsync(createdById);

        ViewBag.TasksPerUserLabels = labels;
        ViewBag.TasksPerUserCounts = counts;
        ViewBag.TasksPerUserIds = userIds;

        // Role counts (global, not filtered)
        var roleCounts = new Dictionary<string, int>
                {
                    { StaticDetails.Roles.Admin, 1},
                    { StaticDetails.Roles.Manager,(await _userManager.GetUsersInRoleAsync(StaticDetails.Roles.Manager))
                                                .Count(u => u.CreatedBy == createdById) },
                    { StaticDetails.Roles.Member, (await _userManager.GetUsersInRoleAsync(StaticDetails.Roles.Member))
                                                .Count(u => u.CreatedBy == createdById)}
                };

        ViewBag.RoleLabels = roleCounts.Keys.ToList();
        ViewBag.RoleCounts = roleCounts.Values.ToList();

        return View();
    }
    private async Task<(string[] Labels, int[] Counts, string[] userIds)> GetTasksPerUserAsync(string userId)
    {
        var members = (await _userManager.GetUsersInRoleAsync(StaticDetails.Roles.Member))
                       .Where(u => u.CreatedBy == userId)
                       .ToList();

        var assignments = await _context.TaskAssignments.Where(ta => members.Select(m => m.Id).Contains(ta.AssignedTo) && ta.TaskUpdates.All(t => t.Status != (int)TaskPro.Models.Enum.TaskStatus.Completed))
            .Include(ta => ta.AssignedToNavigation)
            .ToListAsync();

        var tasksPerUser = members
            .Select(u => new
            {
                UserName = u.FullName ?? u.UserName,  // fallback to username
                TaskCount = assignments.Count(a => a.AssignedTo == u.Id),
                Id = u.Id
            })
            .ToList();

        return (
            tasksPerUser.Select(x => x.UserName).ToArray(),
            tasksPerUser.Select(x => x.TaskCount).ToArray(),
            tasksPerUser.Select(x => x.Id).ToArray()
        );
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
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/Identity/Account/Login");
    }

    [HttpGet]
    [Authorize]
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
        //return Json(new List<object>());
    }
}

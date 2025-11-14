using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using TaskPro.Helper;
using TaskPro.Models;

namespace TaskPro.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly TaskProDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(TaskProDbContext context, UserManager<ApplicationUser> userManager)
        {
            _db = context;
            _userManager = userManager;
        }
        [HttpGet]
        public async Task<IActionResult> TaskReport()
        {
            var model = new TaskReportPageViewModel
            {
                Filter = new TaskReportFilter(),
                Results = new List<TaskReportViewModel>(),
                AvailableUsers = await GetAvailableUsersAsync(),
                TaskStatusList = new SelectList(
                                     System.Enum.GetValues(typeof(Models.Enum.TaskStatus))
                                        .Cast<Models.Enum.TaskStatus>()
                                        .Select(s => new { Value = (int)s, Text = s.ToString() }),
                                    "Value", "Text")

            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> TaskReport(TaskReportPageViewModel model)
        {
            model.TaskStatusList = new SelectList(
                                   System.Enum.GetValues(typeof(Models.Enum.TaskStatus))
                                        .Cast<Models.Enum.TaskStatus>()
                                        .Select(s => new { Value = (int)s, Text = s.ToString() }),
                                    "Value", "Text"
            );

            model.AvailableUsers = await GetAvailableUsersAsync();

            model.Results = await GenerateTaskReportAsync(model.Filter);
            return View(model);
        }
        private async Task<List<TaskReportViewModel>> GenerateTaskReportAsync(TaskReportFilter filter)
        {
            var tasksQuery = _db.Tasks
                                .Include(t => t.TaskAssignments)
                                    .ThenInclude(a => a.AssignedToNavigation)
                                .Include(t => t.TaskAssignments)
                                    .ThenInclude(a => a.TaskUpdates)
                                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.UserId))
                tasksQuery = tasksQuery.Where(t => t.TaskAssignments.Any(r => r.AssignedTo == filter.UserId));

            if (filter.FromDate.HasValue)
                tasksQuery = tasksQuery.Where(t => t.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                tasksQuery = tasksQuery.Where(t => t.CreatedAt <= filter.ToDate.Value);

            if (filter.Status.HasValue)
                tasksQuery = tasksQuery.Where(t =>
                    t.TaskAssignments.Any(a =>
                        a.TaskUpdates.Any(u => u.Status == (int)filter.Status.Value)));

            var tasks = await tasksQuery.ToListAsync();

            var report = new List<TaskReportViewModel>();
            foreach (var task in tasks)
            {
                var assignments = string.IsNullOrEmpty(filter.UserId) ? task.TaskAssignments : task.TaskAssignments.Where(a => a.AssignedTo == filter.UserId);
                foreach (var assignment in assignments)
                {
                    var latestUpdate = assignment.TaskUpdates
                        .OrderByDescending(u => u.UpdatedAt)
                        .FirstOrDefault();

                    report.Add(new TaskReportViewModel
                    {
                        TaskTitle = task.Title,
                        AssignedUser = assignment.AssignedToNavigation?.FullName ?? "N/A",
                        Status = latestUpdate != null ? ((Models.Enum.TaskStatus)latestUpdate.Status).ToString() : "N/A",
                        CreatedDate = task.CreatedAt,
                        CompletedDate = latestUpdate?.UpdatedAt
                    });
                }
            }

            return report;
        }

        [HttpPost]
        public async Task<IActionResult> ExportToCsv(TaskReportFilter filter)
        {
            var report = await GenerateTaskReportAsync(filter);

            var csv = new StringBuilder();
            csv.AppendLine("Task Title,Assigned User,Status,Created Date,LastUpdated Date");

            foreach (var r in report)
            {
                csv.AppendLine($"{EscapeCsv(r.TaskTitle)},{EscapeCsv(r.AssignedUser)},{EscapeCsv(r.Status)},{r.CreatedDate:yyyy-MM-dd},{r.CompletedDate:yyyy-MM-dd}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "TaskReport.csv");
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private async Task<List<SelectListItem>> GetAvailableUsersAsync()
        {
            bool isAdmin = User.IsInRole(StaticDetails.Roles.Admin);
            var createdById = isAdmin ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value : User.FindFirst(ClaimTypes.PrimarySid)?.Value;

            var users = await _userManager.Users.Where(u => u.CreatedBy == createdById).ToListAsync();
            var availableUsers = new List<SelectListItem>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains(StaticDetails.Roles.Admin) && !roles.Contains(StaticDetails.Roles.Manager))
                {
                    availableUsers.Add(new SelectListItem
                    {
                        Value = user.Id,
                        Text = user.FullName
                    });
                }
            }
            return availableUsers;
        }

    }
}
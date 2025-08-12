using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPro.Helper;
using TaskPro.Models;

namespace TaskPro.Controllers
{
    [Authorize(Roles = StaticDetails.Roles.Member)]
    public class MemberController : Controller
    {
        private readonly TaskProDbContext _db = new();
        public MemberController(TaskProDbContext db) => _db = db;

        public IActionResult Index(string priority, string status, string search, DateTime? fromDate, DateTime? toDate)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login", new { returnUrl = Url.Action("Index", "Home") });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var taskAssignments = _db.TaskAssignments
                .Where(t => t.AssignedTo == userId)
                .Include(t => t.TaskUpdates)
                .Include(t => t.Task)
                .ThenInclude(t => t.CreatedByNavigation)
                .AsQueryable();

            // Filter by Priority
            if (!string.IsNullOrEmpty(priority) && System.Enum.TryParse(priority, out TaskPro.Models.Enum.TaskPriority priorityEnum))
            {
                taskAssignments = taskAssignments.Where(t => t.Task.Priority == (int)priorityEnum);
            }

            // Filter by Status
            if (!string.IsNullOrEmpty(status) && System.Enum.TryParse(status, out TaskPro.Models.Enum.TaskStatus statusEnum))
            {
                taskAssignments = taskAssignments.Where(t => t.TaskUpdates.Any(u => u.Status == (int)statusEnum));
            }

            // Search by Title
            if (!string.IsNullOrWhiteSpace(search))
            {
                taskAssignments = taskAssignments.Where(t => t.Task.Title.Contains(search));
            }

            // Filter by Deadline Date range
            if (fromDate.HasValue)
            {
                taskAssignments = taskAssignments.Where(t => t.Task.Deadline.Date >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                taskAssignments = taskAssignments.Where(t => t.Task.Deadline.Date <= toDate.Value.Date);
            }

            var taskList = taskAssignments
                .OrderByDescending(t => t.Task.CreatedAt)
                .ToList();

            var model = new MemberTasksViewModel
            {
                UpcomingTasks = taskList.Where(t => t.TaskUpdates.Any(u => u.Status == (int)TaskPro.Models.Enum.TaskStatus.Pending)).ToList(),
                InProgressTasks = taskList.Where(t => t.TaskUpdates.Any(u => u.Status == (int)TaskPro.Models.Enum.TaskStatus.InProgress)).ToList(),
                CompletedTasks = taskList.Where(t => t.TaskUpdates.Any(u => u.Status == (int)TaskPro.Models.Enum.TaskStatus.Completed)).ToList()
            };

            return View(model);
        }

        // GET: MemberTasks/Details/5
        public IActionResult TaskDetail(int id)
        {
            var taskAssignment = _db.TaskAssignments
                .Include(t => t.Task)
                    .ThenInclude(t => t.CreatedByNavigation)
                .Include(t => t.Task)
                    .ThenInclude(t => t.TaskAssignments)
                        .ThenInclude(a => a.AssignedToNavigation)
                .Include(t => t.TaskUpdates)
                .FirstOrDefault(t => t.Id == id);

            if (taskAssignment == null)
            {
                return NotFound();
            }

            return View(taskAssignment);
        }

        [HttpPost]
        public IActionResult UpdateTaskStatus(int assignmentId, TaskPro.Models.Enum.TaskStatus status)
        {
            // Find the task assignment by assignmentId
            var taskAssignment = _db.TaskAssignments
                .Include(t => t.TaskUpdates)
                .FirstOrDefault(t => t.Id == assignmentId);

            if (taskAssignment == null)
            {
                return NotFound();
            }
            var taskStatus = taskAssignment.TaskUpdates.FirstOrDefault();
            if (taskStatus is not null)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                taskStatus.Status = (int)status;
                taskStatus.UpdatedAt = DateTime.UtcNow;
                taskStatus.UpdatedBy = userId;
            }

            _db.TaskUpdates.Update(taskStatus);
            _db.SaveChanges();

            // Redirect back to TaskDetail view with the same assignmentId
            return RedirectToAction("TaskDetail", new { id = assignmentId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsReadAsync(int notificationId)
        {
            var notification = await _db.Notifications.FindAsync(notificationId);
            if (notification != null && !(notification.IsRead ?? false))
            {
                notification.IsRead = true;
                await _db.SaveChangesAsync();
            }
            return Ok();
        }
    }
}


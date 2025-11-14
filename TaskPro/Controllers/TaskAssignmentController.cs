using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPro.Helper;
using TaskPro.Models;

namespace TaskPro.Controllers
{
    [Authorize]
    public class TaskAssignmentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TaskProDbContext _db;
        private readonly INotificationService _notificationService;

        public TaskAssignmentController(UserManager<ApplicationUser> userManager, TaskProDbContext db, INotificationService notificationService)
        {
            _userManager = userManager;
            _db = db;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> TaskDetails(int id)
        {
            var task = await _db.Tasks.FindAsync(id);
            if (task == null)
                return NotFound();
            List<SelectListItem> availableUsers = await GetAvailableUsers();

            var assignedUsers = await _db.TaskAssignments
                                        .Where(t => t.TaskId == id)
                                        .Include(t => t.AssignedToNavigation)
                                        .Include(t => t.TaskUpdates)
                                        .ToListAsync();

            var viewModel = new TaskDetailViewModel
            {
                Task = task,
                AvailableUsers = availableUsers,
                AssignedUsers = assignedUsers
            };

            return View(viewModel);
        }

        private async Task<List<SelectListItem>> GetAvailableUsers()
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignUser(int taskId, string selectedUserId)
        {
            if (string.IsNullOrEmpty(selectedUserId))
                return RedirectToAction("TaskDetails", new { id = taskId });

            var alreadyAssigned = await _db.TaskAssignments
                                          .AnyAsync(t => t.TaskId == taskId && t.AssignedTo == selectedUserId);

            if (!alreadyAssigned)
            {
                var assignment = new TaskAssignment
                {
                    TaskId = taskId,
                    AssignedTo = selectedUserId,
                    AssignedAt = DateTime.UtcNow
                };

                _db.TaskAssignments.Add(assignment);
                await _db.SaveChangesAsync();

                var taskStatus = new TaskUpdate
                {
                    TaskAssignmentId = assignment.Id,
                    Status = (int)Models.Enum.TaskStatus.Pending,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.TaskUpdates.Add(taskStatus);
                await _db.SaveChangesAsync();

                var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);
                if (task is not null)
                    await _notificationService.AddNotificationAsync(selectedUserId, $"You have been assigned a new task {task.Title}");
            }

            return RedirectToAction("TaskDetails", new { id = taskId });
        }

        [HttpPost]
        public async Task<IActionResult> UnassignTask(int taskId, string userId)
        {
            // Your logic here to remove the user from task
            var taskUser = await _db.TaskAssignments
                                    .Include(t=>t.TaskUpdates)
                                    .FirstOrDefaultAsync(x => x.TaskId == taskId && x.AssignedTo == userId);

            if (taskUser != null)
            {
                _db.TaskUpdates.RemoveRange(taskUser.TaskUpdates);

                _db.TaskAssignments.Remove(taskUser);
                await _db.SaveChangesAsync();

                var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId);

                if (task != null)               
                    await _notificationService.AddNotificationAsync(
                        userId,
                        $"You have been unassigned from the task '{task.Title}'"
                    );        
            }
            return RedirectToAction("TaskDetails", new { id = taskId });
        }

        public async Task<IActionResult> UserTask(string? userId)
        {
            bool isAdmin = User.IsInRole(StaticDetails.Roles.Admin);
            var createdById = isAdmin ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value : User.FindFirst(ClaimTypes.PrimarySid)?.Value;
            var managerIds = (await _userManager.GetUsersInRoleAsync(StaticDetails.Roles.Manager))
                            .Where(u => u.CreatedBy == createdById)
                            .Select(u => u.Id)
                            .ToList();

            // Admin sees tasks created by self OR those managers
            var query = _db.TaskAssignments
                            .Include(t => t.AssignedToNavigation)
                            .Include(t => t.Task) // assuming Task entity
                            .AsQueryable();

            query = query.Where(t => managerIds.Contains(t.Task.CreatedBy) || t.Task.CreatedBy == createdById);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(t => t.AssignedTo == userId);
            
            var model = await query
                .Select(t => new UserTaskViewModel
                {
                    UserId = t.AssignedTo,
                    UserName = t.AssignedToNavigation.FullName,
                    TaskId = t.TaskId,
                    TaskTitle = t.Task.Title,
                    Status =( TaskPro.Models.Enum.TaskStatus)t.TaskUpdates.First().Status,
                    DueDate = t.Task.Deadline
                })
                .ToListAsync();

            ViewBag.Users = await GetAvailableUsers();
            return View(model);
        }
    }
}
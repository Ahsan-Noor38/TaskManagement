﻿using Microsoft.AspNetCore.Authorization;
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

        public TaskAssignmentController(UserManager<ApplicationUser> userManager, TaskProDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> TaskDetails(int id)
        {
            var task = await _db.Tasks.FindAsync(id);
            if (task == null)
                return NotFound();

            bool isAdmin = User.IsInRole(StaticDetails.Roles.Admin);
            var createdById = isAdmin ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value :User.FindFirst(ClaimTypes.PrimarySid)?.Value;

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

            var assignedUsers = await _db.TaskAssignments
                                        .Where(t => t.TaskId == id)
                                        .Include(t => t.AssignedToNavigation)
                                        .ToListAsync();

            var viewModel = new TaskDetailViewModel
            {
                Task = task,
                AvailableUsers = availableUsers,
                AssignedUsers = assignedUsers
            };

            return View(viewModel);
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
            }

            return RedirectToAction("TaskDetails", new { id = taskId });
        }

        [HttpPost]
        public async Task<IActionResult> UnassignTask(int taskId, string userId)
        {
            // Your logic here to remove the user from task
            var taskUser = await _db.TaskAssignments
                                    .FirstOrDefaultAsync(x => x.TaskId == taskId && x.AssignedTo == userId);

            if (taskUser != null)
            {
                _db.TaskAssignments.Remove(taskUser);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("TaskDetails", new { id = taskId });
        }

    }
}
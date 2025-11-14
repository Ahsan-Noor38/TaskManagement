using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPro.Helper;
using TaskPro.Models;
using Task = TaskPro.Models.Task;

namespace TaskPro.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly TaskProDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TasksController(TaskProDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Tasks
        public async Task<IActionResult> Index(Models.Enum.TaskStatus? statusFilter , bool? isOverdue)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value; // Assuming Role is stored in claims

            IQueryable<Task> query = _context.Tasks
                .Include(t => t.CreatedByNavigation)
                .Include(t => t.TaskAssignments)
                .ThenInclude(t => t.TaskUpdates);

            if (userRole == StaticDetails.Roles.Admin)
            {
                // get all managers + admin userIds
                // Admin sees all tasks created by managers or admins
                var managerIds = (await _userManager.GetUsersInRoleAsync(StaticDetails.Roles.Manager))
                                    .Where(u => u.CreatedBy == userId)
                                    .Select(u => u.Id)
                                    .ToList();

                query = query.Where(t => managerIds.Contains(t.CreatedBy) || t.CreatedBy == userId);
            }
            else
                query = query.Where(t => t.CreatedBy == userId);

            var taskProDbContext = await query.ToListAsync();
            taskProDbContext.ForEach(r =>
                r.Description = r.Description.Length > 15
                    ? r.Description.Substring(0, 15) + "..."
                    : r.Description);

            // Apply status filter if selected
            if (statusFilter.HasValue)
            {
                taskProDbContext = taskProDbContext.Where(task =>
                {
                    var statuses = task.TaskAssignments
                                       .SelectMany(t => t.TaskUpdates)
                                       .Select(u => u.Status)
                                       .ToList();

                    Models.Enum.TaskStatus overallStatus;
                    if (statuses.All(s => s == (int)Models.Enum.TaskStatus.Pending))
                        overallStatus = Models.Enum.TaskStatus.Pending;
                    else if (statuses.All(s => s == (int)Models.Enum.TaskStatus.Completed))
                        overallStatus = Models.Enum.TaskStatus.Completed;
                    else
                        overallStatus = Models.Enum.TaskStatus.InProgress;

                    return overallStatus == statusFilter.Value;
                }).ToList();
            }

            if (isOverdue.HasValue)
                taskProDbContext = taskProDbContext.Where(t => t.Deadline.Date < DateTime.UtcNow.Date).ToList();
            
            // Create dictionary of { taskId → isAssigned }
            var taskUsage = await _context.Tasks
                .Select(t => new
                {
                    t.Id,
                    IsAssigned = _context.TaskAssignments.Any(a => a.TaskId == t.Id)
                })
                .ToDictionaryAsync(t => t.Id, t => t.IsAssigned);

            ViewBag.TaskUsage = taskUsage;
            ViewBag.StatusFilter = statusFilter; // keep selected status
            ViewBag.IsOverdue = isOverdue;

            return View(taskProDbContext);
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.CreatedByNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // GET: Tasks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tasks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Task task)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            task.CreatedAt = DateTime.Now;
            task.CreatedBy = userId;

            _context.Add(task);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            return View(task);
        }

        // POST: Tasks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Task task)
        {
            if (id != task.Id)
                return NotFound();

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var taskEntity = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

                if (taskEntity is null)
                    return NotFound();

                taskEntity.Title = task.Title;
                taskEntity.Description = task.Description;
                taskEntity.Priority = task.Priority;
                taskEntity.Deadline = task.Deadline;
                taskEntity.UpdatedBy = userId;
                taskEntity.UpdatedDate = DateTime.UtcNow;

                _context.Update(taskEntity);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(task.Id))
                    return NotFound();

                else
                    throw;

            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.CreatedByNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}

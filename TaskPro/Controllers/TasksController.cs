using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPro.Models;
using Task = TaskPro.Models.Task;

namespace TaskPro.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly TaskProDbContext _context;

        public TasksController(TaskProDbContext context)
        {
            _context = context;
        }

        // GET: Tasks
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var taskProDbContext = _context.Tasks.Where(t => t.CreatedBy == userId).Include(t => t.CreatedByNavigation);
            await taskProDbContext.ForEachAsync(r => r.Description = r.Description.Length > 15 ? r.Description.Substring(0, 15) + "..." : r.Description);

            // Create dictionary of { taskId → isAssigned }
            var taskUsage = await _context.Tasks
                                            .Select(t => new
                                            {
                                                t.Id,
                                                IsAssigned = _context.TaskAssignments.Any(a => a.TaskId == t.Id)
                                            })
                                            .ToDictionaryAsync(t => t.Id, t => t.IsAssigned);

            ViewBag.TaskUsage = taskUsage;
            return View(await taskProDbContext.ToListAsync());
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

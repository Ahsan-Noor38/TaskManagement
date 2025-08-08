using Microsoft.AspNetCore.Mvc.Rendering;
using TaskPro.Models;
using static TaskPro.Models.Enum;
using Task = TaskPro.Models.Task;

public class TaskDetailViewModel
{
    public Task Task { get; set; }

    // For assigning
    public string SelectedUserId { get; set; }
    public List<SelectListItem> AvailableUsers { get; set; }

    // Existing assignments (optional)
    public List<TaskAssignment> AssignedUsers { get; set; }

    public string Priority => ((TaskPriority)Task.Priority).ToString();
    public string PriorityCssClass => Priority.ToLowerInvariant(); 
}


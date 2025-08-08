using System;
using System.Collections.Generic;

namespace TaskPro.Models;

public partial class TaskAssignment
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public string AssignedTo { get; set; } = null!;

    public DateTime? AssignedAt { get; set; }

    public virtual AspNetUser AssignedToNavigation { get; set; } = null!;

    public virtual Task Task { get; set; } = null!;

    public virtual ICollection<TaskUpdate> TaskUpdates { get; set; } = new List<TaskUpdate>();
}

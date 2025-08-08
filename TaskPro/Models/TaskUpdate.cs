using System;
using System.Collections.Generic;

namespace TaskPro.Models;

public partial class TaskUpdate
{
    public int Id { get; set; }

    public int TaskAssignmentId { get; set; }

    public string? UpdatedBy { get; set; } = null!;

    public int Status { get; set; }

    public string? Comment { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual TaskAssignment TaskAssignment { get; set; } = null!;

    public virtual AspNetUser UpdatedByNavigation { get; set; } = null!;
}

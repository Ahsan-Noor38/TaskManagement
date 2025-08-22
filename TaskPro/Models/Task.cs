using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPro.Models;

public partial class Task
{
    public int Id { get; set; }
    [Required]
    public string Title { get; set; } = null!;
    [Required]
    public string Description { get; set; } = null!;
    [Required
    public int Priority { get; set; }
    [Required]
    public DateTime Deadline { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual AspNetUser CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}

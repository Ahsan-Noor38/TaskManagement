namespace TaskHive.Models
{
    using System;

    public partial class TaskAssignment
    {
        public int Id { get; set; }

        public int TaskId { get; set; }

        public int AssignedTo { get; set; }

        public DateTime? AssignedAt { get; set; }

        public virtual User User { get; set; }

        public virtual Task Task { get; set; }
    }
}

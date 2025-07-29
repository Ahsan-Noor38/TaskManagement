namespace TaskHive.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Task
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Task()
        {
            TaskAssignments = new HashSet<TaskAssignment>();
            TaskUpdates = new HashSet<TaskUpdate>();
        }

        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        [StringLength(10)]
        public string Priority { get; set; }

        public DateTime? Deadline { get; set; }

        [StringLength(20)]
        public string Status { get; set; }

        public int CreatedBy { get; set; }

        public DateTime? CreatedAt { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TaskAssignment> TaskAssignments { get; set; }

        public virtual User User { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TaskUpdate> TaskUpdates { get; set; }
    }
}

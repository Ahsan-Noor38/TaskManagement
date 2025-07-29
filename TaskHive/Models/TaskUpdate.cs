namespace TaskHive.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class TaskUpdate
    {
        public int Id { get; set; }

        public int TaskId { get; set; }

        public int UpdatedBy { get; set; }

        [StringLength(20)]
        public string Status { get; set; }

        public string Comment { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual Task Task { get; set; }

        public virtual User User { get; set; }
    }
}

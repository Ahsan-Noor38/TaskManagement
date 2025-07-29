namespace TaskHive.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [StringLength(255)]
        public string Message { get; set; }

        public bool? IsRead { get; set; }

        public DateTime? CreatedAt { get; set; }

        public virtual User User { get; set; }
    }
}

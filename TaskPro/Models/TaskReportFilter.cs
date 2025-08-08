namespace TaskPro.Models
{
    public class TaskReportFilter
    {
        public string? UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Enum.TaskStatus? Status { get; set; } // Assuming TaskStatus is an enum
    }

}

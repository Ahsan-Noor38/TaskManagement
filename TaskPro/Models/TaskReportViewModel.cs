namespace TaskPro.Models
{
    public class TaskReportViewModel
    {
        public string TaskTitle { get; set; }
        public string AssignedUser { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}

namespace TaskPro.Models
{
    public class UserTaskViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; }
        public Models.Enum.TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
    }

}

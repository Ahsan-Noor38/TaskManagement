namespace TaskPro.Models
{
    public class MemberTasksViewModel
    {
        public List<TaskAssignment> UpcomingTasks { get; set; }
        public List<TaskAssignment> InProgressTasks { get; set; }
        public List<TaskAssignment> CompletedTasks { get; set; }
    }

}

using Microsoft.AspNetCore.Mvc.Rendering;

namespace TaskPro.Models
{
    public class TaskReportPageViewModel
    {
        public TaskReportFilter Filter { get; set; } = new();
        public List<TaskReportViewModel> Results { get; set; } = new(); 
        public SelectList TaskStatusList { get; set; }   // For status dropdown

        public List<SelectListItem> AvailableUsers { get; set; } // For users dropdown

    }
}

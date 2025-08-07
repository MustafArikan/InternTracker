namespace InternTracker.Models
{
    public class TaskItemActionViewModel : TaskItem
    {
        public bool CanStart { get; set; }
        public bool CanStop { get; set; }
        public bool IsActive { get; set; }
    }
}
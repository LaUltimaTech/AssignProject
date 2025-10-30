using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignProject.Models
{
    [Table("tbl_task_reminder")]
    public class TaskReminder
    {
        [Key]
        public int Task_Reminder_ID { get; set; }
        public int Task_Unique_ID { get; set; }
        public string Message_Description { get; set; }
        public DateTime Trigger_Timestamp { get; set; }
        public bool Reminder_Flag { get; set; } = true;

        [ForeignKey("Task_Unique_ID")]
        public AssignTask Task { get; set; }
        // Navigation property
    }

}

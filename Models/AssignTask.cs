using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignProject.Models
{
    [Table("AssignTask")]
    public class AssignTask
    {
        [Key]
        public int Task_Unique_ID { get; set; }

        [Required]
        public int Task_Number { get; set; }

        [Required, StringLength(500)]
        public string Task_Description { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime Task_Assigned_Date { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime Approx_Completion_Date { get; set; }

        public bool IsActive { get; set; } = true;

        public string WhatsAppStatus { get; set; } = "Active";

        // Navigation Property
        public ICollection<Task_To_Employee> TaskMappings { get; set; }
    }

}

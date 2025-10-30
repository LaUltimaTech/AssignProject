using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignProject.Models
{
    [Table("Task_To_Employee")]
    public class Task_To_Employee
    {
        [Key]
        public int Task_to_emp_Uniq_ID { get; set; }

        public int Task_Unique_ID { get; set; }
        public AssignTask Task { get; set; }

        public int Employee_ID { get; set; }
        public Employee Employee { get; set; }
    }
}

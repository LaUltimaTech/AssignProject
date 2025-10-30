using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignProject.Models
{
    [Table("Employee")]
    public class Employee
    {
        [Key]
        public int Employee_ID { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [StringLength(50)]
        public string Employee_First_Name { get; set; }

        [StringLength(50)]
        public string Employee_Middle_Name { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(50)]
        public string Employee_Last_Name { get; set; }

        
        public long Employee_WhatsApp_Number { get; set; }

        public bool IsActive { get; set; } = true;

        // ✅ Navigation property initialized to avoid null errors
        public ICollection<Task_To_Employee> TaskMappings { get; set; } = new List<Task_To_Employee>();
    }
}

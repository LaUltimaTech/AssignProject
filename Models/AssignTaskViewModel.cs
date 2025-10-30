using System;
using System.Collections.Generic;

namespace AssignProject.Models   // <-- tujhya project namespace verify kar
{
    public class AssignTaskViewModel
    {
        public int Task_Unique_ID { get; set; }
        public int Task_Number { get; set; }
        public string Task_Description { get; set; }
        public DateTime Task_Assigned_Date { get; set; }
        public DateTime Approx_Completion_Date { get; set; }

        public string WhatsAppStatus { get; set; }


        // For multi-select in form
        public List<int> SelectedEmployeeIds { get; set; } = new List<int>();
        public List<Employee> AvailableEmployees { get; set; } = new List<Employee>();

        // For table display
        public string AssignedEmployeeNames { get; set; }
    }
}

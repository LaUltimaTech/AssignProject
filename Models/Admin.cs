using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssignProject.Models
{
    [Table("AdminLogin")]
    public class Admin
    {
        [Key] 
        public int admin_id { get; set; }   

        public string username { get; set; }

        public string password { get; set; }    
    }
}

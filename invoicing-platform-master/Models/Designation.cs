using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client_Invoice_System.Models
{
    public class Designation
    {
        [Key]
        public int Id { get; set; }
        [Column("Designation")]
        public string DesignationName { get; set; } 
    }
}

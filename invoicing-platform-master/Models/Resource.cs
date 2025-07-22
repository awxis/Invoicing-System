using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Client_Invoice_System.Models
{
    public class Resource : ISoftDeletable
    {
        public enum RecurrenceType
        {
            Monthly,
            Weekly
        }

        [Key]
        public int ResourceId { get; set; }

        [ForeignKey("Client")]
        [Required(ErrorMessage = "Client Selection is required.")]
        public int ClientId { get; set; }

        [Required]
        public string ResourceName { get; set; }

        [ForeignKey("Employee")]
        public int EmployeeId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public decimal CommittedHours { get; set; }

        public RecurrenceType Recurrence { get; set; }

        // Navigation Properties
        public virtual Client Client { get; set; }
        public virtual Employee Employee { get; set; }

        public int OwnerProfileId { get; set; }

        [ForeignKey("OwnerProfileId")]
        public virtual OwnerProfile OwnerProfile { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
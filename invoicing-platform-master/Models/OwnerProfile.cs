using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client_Invoice_System.Models
{
    public class OwnerProfile : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Owner Name is required")]
        [StringLength(100, ErrorMessage = "Owner Name cannot exceed 100 characters.")]
        public string OwnerName { get; set; }

        [Required(ErrorMessage = "Billing Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string BillingEmail { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        public string? PhoneNumber { get; set; } // Optional

        [Required(ErrorMessage = "Billing Address is required")]
        [StringLength(500, ErrorMessage = "Billing Address cannot exceed 500 characters.")]
        public string BillingAddress { get; set; } // Optional

        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid country and currency.")]
        public int CountryCurrencyId { get; set; }

        [ForeignKey("CountryCurrencyId")]
        public virtual CountryCurrency CountryCurrency { get; set; }

        public string? CustomCurrency { get; set; } // Optional

        public byte[]? Logo { get; set; } // Optional

        public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();
        public virtual ICollection<OwnerBankAccount> BankAccounts { get; set; } = new List<OwnerBankAccount>();

        public bool IsDeleted { get; set; } = false;
    }
}
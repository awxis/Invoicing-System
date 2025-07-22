using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client_Invoice_System.Models
{
    public class OwnerBankAccount : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OwnerProfileId { get; set; }
        [ForeignKey("OwnerProfileId")]
        public virtual OwnerProfile OwnerProfile { get; set; }

        [Required]
        [StringLength(100)] // Reasonable length for a label
        public string Label { get; set; }

        [Required]
        [StringLength(50)]  // Reasonable length for an account number
        public string AccountNumber { get; set; }

        [Required]
        public int CurrencyId { get; set; }
        [ForeignKey("CurrencyId")]
        public virtual CountryCurrency CountryCurrency { get; set; }

        [StringLength(100)] // Reasonable length for a bank name
        public string? BankName { get; set; }

        [StringLength(100)] // Reasonable length for an account title
        public string? AccountTitle { get; set; }

        [StringLength(50)]  // IBANs have a max length, e.g., 34
        public string? IBAN { get; set; }

        [StringLength(20)]  // SWIFT codes are typically 8 or 11 characters
        public string? SwiftCode { get; set; }

        [StringLength(20)]  // Sort codes vary in length
        public string? SortCode { get; set; }

        [StringLength(50)]  // Bank branch codes vary
        public string? BankBranchCode { get; set; }

        [StringLength(100)] // Payment method description
        public string? ReceivingPaymentMethod { get; set; }

        [StringLength(500)] // Specific instructions can be longer
        public string? SpecificBankPaymentInstructions { get; set; }

        // As per instructions, this implies a bank can be in a country different from its currency's primary country.
        // If CountryCurrency.Country is always the bank's country, this might be redundant.
        // For now, implementing as requested.
        public int? CountryId { get; set; }
        [ForeignKey("CountryId")]
        public virtual CountryCurrency BankCountry { get; set; } // Naming it BankCountry to distinguish from Currency's Country

        public bool IsDefault { get; set; } = false;

        // ISoftDeletable properties
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

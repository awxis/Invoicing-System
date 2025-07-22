using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client_Invoice_System.Models
{
    public class Invoice : ISoftDeletable
    {
        [Key]
        public int InvoiceId { get; set; }

        [ForeignKey("Client")]
        public int ClientId { get; set; }
        public virtual Client Client { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmount { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PaidAmount { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? RemainingAmount { get; set; } 

        [Required]
        public int CountryCurrencyId { get; set; }
        [ForeignKey("CountryCurrencyId")]
        public virtual CountryCurrency CountryCurrency { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public string EmailStatus { get; set; } = "Not Sent";
        public DateTime? DueDate { get; set; }
        public InvoiceStatus InvoiceStatuses { get; set; } = InvoiceStatus.Pending;
        public bool IsPaid => InvoiceStatuses == InvoiceStatus.Paid;
        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public bool IsDeleted { get; set; } = false;
        [Column(TypeName = "decimal(18,4)")]
        public decimal TargetCurrencyConversionRate { get; set; } = 1m;
        public byte[]? PaymentGuidelineImage { get; set; }
        public int BankAccountId { get; set; }
        public string Payment_Communication { get; set; } = null;
    }

    public enum InvoiceStatus
    {
        Pending,
        PartiallyPaid,
        Paid
    }

}

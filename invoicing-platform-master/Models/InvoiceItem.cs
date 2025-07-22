using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Client_Invoice_System.Models
{
    public class InvoiceItem : ISoftDeletable
    {
        public enum ContractVariation
        {
            Hourly,
            Fixed
        }

        [Key]
        public int Id { get; set; }

        [ForeignKey("Invoice")]
        public int InvoiceId { get; set; }
        public virtual Invoice Invoice { get; set; }

        [ForeignKey("Resource")]
        public int ResourceId { get; set; }
        public virtual Resource Resource { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ConsumedHours { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? RatePerHour { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalAmount { get; set; }

        public bool IsDeleted { get; set; } = false;

        public string PurposeCode { get; set; }

        public ContractVariation Variation { get; set; } // Added to store variation per invoice item
    }
}
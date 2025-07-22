using System.ComponentModel.DataAnnotations.Schema;

namespace Client_Invoice_System.Models
{
    public class Receipt
    {
        public int Id { get; set; }
        public DateTime Invoice_Generated_Date { get; set; }
        public double TotalAmount { get; set; }
        public DateTime Invoice_paid_Date { get; set; }
        public string Status { get; set; }
        public bool IsDeleted { get; set; } = false;

        [ForeignKey("CurrencyId")]
        public int? CurrencyId { get; set; }
        public virtual CountryCurrency? CountryCurrency { get; set; }

        public int? ClientId { get; set; }
        public virtual Client? Client { get; set; }

    }
}

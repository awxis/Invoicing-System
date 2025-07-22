﻿using Client_Invoice_System.Components.Pages.Invoice_Pages;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client_Invoice_System.Models
{
    public class Client : ISoftDeletable
    {
        [Key]
        public int ClientId { get; set; }
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string Address { get; set; }
        public string PhoneNumber { get; set; } = " ";
        [Required(ErrorMessage = "CountryCurrencyId is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "CountryCurrencyId must be a positive integer.")]
        public int CountryCurrencyId { get; set; }

        [ForeignKey("CountryCurrencyId")]
        public virtual CountryCurrency CountryCurrency { get; set; }

        public string? CustomCurrency { get; set; }
        [Required(ErrorMessage = "Due date is required.")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Now;
        [Required]
        public string ClientIdentifier { get; set; }

        // New field for client-specific invoice series starting number
        [Required(ErrorMessage = "Invoice series starting number is required.")]
        [Range(1, 999999, ErrorMessage = "Invoice series must be between 1 and 999999.")]
        public int InvoiceSeriesStart { get; set; } = 1; // Default to 1, configurable per client

        public virtual ActiveClient ActiveClient { get; set; }
        public virtual ICollection<Resource> Resources { get; set; }
        public virtual ICollection<ClientProfileCrossTable> ClientProfileCrosses { get; set; }
        public List<Invoice> Invoices { get; set; }
        public virtual ICollection<Receipt>? Receipts { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
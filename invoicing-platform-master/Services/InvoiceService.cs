using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Client_Invoice_System.Services
{
    public class InvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(
            ApplicationDbContext context,
            EmailService emailService,
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<InvoiceService> logger)
        {
            _context = context;
            _emailService = emailService;
            _contextFactory = contextFactory;
            _logger = logger;
        }

        // Overload for single resource
        public async Task<int> SaveInvoiceAsync(
            int clientId,
            int resourceId,
            decimal consumedHours,
            decimal rate,
            decimal amount,
            InvoiceItem.ContractVariation variation,
            DateTime? startDate,
            DateTime? endDate,
            DateTime? dueDate,
            string purposeCode,
            decimal conversionRate = 1m,
            byte[] paymentGuidelineImage = null,
            int? ownerCurrencyId = null,
            int bankAccountId = 0,
            string Payment_Communication = "")
        {
            using var context = _contextFactory.CreateDbContext();

            var resource = await context.Resources
                .Include(r => r.Employee)
                .Include(r => r.OwnerProfile)
                .FirstOrDefaultAsync(r => r.ResourceId == resourceId && r.ClientId == clientId);

            if (resource == null)
            {
                _logger.LogError($"Resource {resourceId} not found for ClientId {clientId}.");
                throw new Exception($"Resource {resourceId} not found!");
            }

            // Calculate total amount based on variation and conversion rate
            decimal totalAmount = variation == InvoiceItem.ContractVariation.Hourly
                ? (consumedHours * rate) * conversionRate
                : amount * conversionRate;

            var invoiceItem = new InvoiceItem
            {
                ResourceId = resourceId,
                ConsumedHours = variation == InvoiceItem.ContractVariation.Hourly ? consumedHours : 0m,
                RatePerHour = variation == InvoiceItem.ContractVariation.Hourly ? rate : 0m,
                TotalAmount = totalAmount,
                PurposeCode = purposeCode,
                Variation = variation
            };

            var invoiceItems = new List<InvoiceItem> { invoiceItem };

            return await SaveInvoiceAsync(clientId, invoiceItems, startDate, endDate, dueDate, conversionRate, paymentGuidelineImage, ownerCurrencyId,bankAccountId, Payment_Communication);
        }

        private async Task<int> SaveInvoiceAsync(
            int clientId,
            List<InvoiceItem> invoiceItems,
            DateTime? startDate,
            DateTime? endDate,
            DateTime? dueDate,
            decimal conversionRate = 1m,
            byte[] paymentGuidelineImage = null,
            int? ownerCurrencyId = null,
            int bankAccountId = 0,
            string paymentCommunication = "")
        {
            using var context = _contextFactory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var client = await context.Clients
                    .Include(c => c.CountryCurrency)
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);
                if (client == null)
                {
                    _logger.LogError($"Client with ID {clientId} not found.");
                    throw new Exception("Client not found!");
                }

                foreach (var item in invoiceItems)
                {
                    var resource = await context.Resources
                        .Include(r => r.Employee)
                        .FirstOrDefaultAsync(r => r.ResourceId == item.ResourceId && r.ClientId == clientId);
                    if (resource == null)
                    {
                        _logger.LogError($"Resource {item.ResourceId} not found for ClientId {clientId}.");
                        throw new Exception($"Resource {item.ResourceId} not found!");
                    }

                    // Ensure TotalAmount is correctly calculated, handle nullable fields
                    item.ConsumedHours = item.Variation == InvoiceItem.ContractVariation.Hourly ? (item.ConsumedHours ?? 0m) : 0m;
                    item.RatePerHour = item.Variation == InvoiceItem.ContractVariation.Hourly ? (item.RatePerHour ?? 0m) : 0m;
                    item.TotalAmount = item.Variation == InvoiceItem.ContractVariation.Hourly
                        ? (item.ConsumedHours * item.RatePerHour) * conversionRate
                        : (item.TotalAmount ?? 0m) * conversionRate;
                }

                // Determine the currency ID for the invoice
                int? invoiceCurrencyId = ownerCurrencyId;
                if (!invoiceCurrencyId.HasValue)
                {
                    var firstItem = invoiceItems.FirstOrDefault();
                    if (firstItem != null)
                    {
                        var resource = await context.Resources
                            .Include(r => r.OwnerProfile)
                            .FirstOrDefaultAsync(r => r.ResourceId == firstItem.ResourceId);
                        if (resource?.OwnerProfile?.CountryCurrencyId != null)
                        {
                            invoiceCurrencyId = resource.OwnerProfile.CountryCurrencyId;
                        }
                    }
                }

                var invoice = new Invoice
                {
                    ClientId = clientId,
                    InvoiceDate = endDate?.Date ?? DateTime.UtcNow,
                    StartDate = startDate,
                    EndDate = endDate,
                    DueDate = dueDate,
                    TotalAmount = invoiceItems.Sum(item => item.TotalAmount ?? 0m),
                    CountryCurrencyId = invoiceCurrencyId ?? client.CountryCurrencyId,
                    InvoiceStatuses = InvoiceStatus.Pending,
                    EmailStatus = "Not Sent",
                    TargetCurrencyConversionRate = conversionRate,
                    PaymentGuidelineImage = paymentGuidelineImage,
                    PaidAmount = 0m, // Initialize PaidAmount
                    RemainingAmount = invoiceItems.Sum(item => item.TotalAmount ?? 0m),
                    BankAccountId = bankAccountId,// Initialize RemainingAmount
                    Payment_Communication = paymentCommunication
                };

                context.Invoices.Add(invoice);
                await context.SaveChangesAsync();

                foreach (var item in invoiceItems)
                {
                    item.InvoiceId = invoice.InvoiceId;
                    context.InvoiceItems.Add(item);
                }

                await context.SaveChangesAsync();

                await transaction.CommitAsync();
                _logger.LogInformation($"Invoice {invoice.InvoiceId} saved successfully for ClientId {clientId}.");
                return invoice.InvoiceId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error saving invoice for ClientId {clientId}.");
                throw;
            }
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(int invoiceId, bool receipt = false)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                var invoice = await context.Invoices
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.Client)
                        .ThenInclude(c => c.CountryCurrency)
                    .Include(i => i.CountryCurrency)
                    .Include(i => i.InvoiceItems)
                        .ThenInclude(ii => ii.Resource)
                            .ThenInclude(r => r.Employee)
                              .ThenInclude(d => d.Designation)
                    .Include(i => i.InvoiceItems)
                        .ThenInclude(ii => ii.Resource)
                            .ThenInclude(r => r.OwnerProfile)
                                .ThenInclude(op => op.CountryCurrency)
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
                var bank = context.OwnerBankAccounts.FirstOrDefault(o => o.Id == invoice.BankAccountId);

                if (invoice == null)
                {
                    _logger.LogError($"Invoice with ID {invoiceId} not found.");
                    throw new Exception("Invoice not found!");
                }

                var client = invoice.Client;
                var invoiceItems = invoice.InvoiceItems.ToList();

                if (!invoiceItems.Any())
                {
                    _logger.LogWarning($"No invoice items found for Invoice {invoiceId} in the specified date range.");
                    throw new Exception("No invoice items found for the specified date range!");
                }

                decimal conversionRate = invoice.TargetCurrencyConversionRate != 0 ? invoice.TargetCurrencyConversionRate : 1m;
                decimal totalAmount = 0m;

                string currencyOwnerSymbol = invoice.CountryCurrency?.Symbol ?? (invoiceItems.FirstOrDefault()?.Resource?.OwnerProfile?.CountryCurrency?.Symbol ?? "$");
                string currencyOwnerName = invoice.CountryCurrency?.CurrencyName ?? (invoiceItems.FirstOrDefault()?.Resource?.OwnerProfile?.CountryCurrency?.CurrencyName ?? "USD");
                string targetCurrencySymbol = client?.CountryCurrency?.Symbol ?? "$";
                string targetCurrencyName = client?.CountryCurrency?.CurrencyName ?? "USD";

                var ownerProfile = invoiceItems.FirstOrDefault()?.Resource?.OwnerProfile ?? new OwnerProfile
                {
                    OwnerName = "Default Owner",
                    BillingEmail = "default@email.com",
                    PhoneNumber = "+923000000000",
                    BillingAddress = "Default Billing Address",
                    CountryCurrency = new CountryCurrency { CurrencyCode = "USD", Symbol = "$", CurrencyName = "USD" },
                    // BankName = "Default Bank", // Removed property
                    // Swiftcode = "DFLTUS33XXX", // Removed property
                    // AccountTitle = "Default Account", // Removed property
                    // BranchAddress = "Default Branch Address", // Removed property
                    // AccountNumber = "0000000000", // Removed property
                    Logo = null
                };

                // TODO: Select an appropriate OwnerBankAccount from ownerProfile.BankAccounts for PDF generation.
                // For now, using placeholder values or commenting out direct usage.
                var selectedBankAccount = ownerProfile.BankAccounts?.FirstOrDefault(b => b.IsDefault) ?? 
                                      ownerProfile.BankAccounts?.FirstOrDefault();


                using MemoryStream ms = new MemoryStream();
                QuestPDF.Settings.License = LicenseType.Community;

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontFamily("Calibri").FontSize(10));

                        // Add watermark if receipt is true
                        if (receipt)
                        {
                            page.Background()
                                .AlignCenter()
                                .AlignMiddle()
                                .Rotate(-48)
                                .Text(text =>
                                {
                                    text.Span("PAID")
                                       .FontColor(Colors.Green.Lighten2.WithAlpha((byte)(0.3f * 255)))
                                        .FontSize(120)
                                        .Bold();
                                });
                        }

                        page.Header().Table(table =>
                        {
                            string headerColor1 = "#1B3942";

                            if (ownerProfile.Logo != null && ownerProfile.Logo.Length > 0)
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(60);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(120);
                                });

                                table.Cell().Border(0.5f).Padding(2).Image(ownerProfile.Logo).FitArea();
                            }
                            else
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(120);
                                });
                            }

                            table.Cell().Border(0.5f).Padding(2).AlignLeft().Column(col =>
                            {
                                col.Item().Text("From").FontSize(12).Bold();
                                col.Item().Text(ownerProfile.OwnerName).FontColor(Color.FromHex(headerColor1)).FontSize(10).Bold();
                                col.Item().Text(ownerProfile.BillingAddress);
                            });

                            table.Cell().Border(0.5f).Padding(2).AlignLeft().Column(col =>
                            {
                                col.Item().Text("To").FontSize(12).Bold();
                                col.Item().Text(client.Name).FontColor(Color.FromHex(headerColor1)).Bold();
                                col.Item().Text(client.Address ?? "N/A");
                            });

                            table.Cell().Border(0.5f).Padding(2).AlignLeft().Column(col =>
                            {
                                string documentType = receipt ? "Receipt" : "Invoice";
                                col.Item().Text(documentType).FontSize(12).Bold();
                                int clientInvoiceBase = client.InvoiceSeriesStart;
                                int invoiceNumber = clientInvoiceBase + invoiceId;
                                string paddedInvoiceNumber = invoiceNumber.ToString("D6");
                                col.Item().Text($"{(receipt ? "RCPT" : "INV")}/{invoice.InvoiceDate.Year}/{paddedInvoiceNumber}")
                                    .FontColor(Color.FromHex(headerColor1)).FontSize(10).Bold();
                                col.Item().Text($"Date: {invoice.InvoiceDate:MM/dd/yyyy}");
                            });
                        });

                        page.Content().Column(col =>
                        {
                            col.Item().PaddingTop(20);
                            string headerColor = "#1B3942";
                            // col.Item().Container().PaddingBottom(5).Text($"Payment Instructions ({ownerProfile.PaymentInstruction})").FontSize(12).FontColor(Color.FromHex(headerColor)).Bold(); // Removed property
                                col.Item().Container().PaddingBottom(5).Text($"Payment Instructions: {(selectedBankAccount?.SpecificBankPaymentInstructions)}").FontSize(12).FontColor(Color.FromHex(headerColor)).Bold();


                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(120);
                                    columns.RelativeColumn();
                                });

                                void AddPaymentRow(string label, string value)
                                {
                                    table.Cell().Padding(2).Text(label).Bold();
                                    table.Cell().Padding(2).Text(value ?? "N/A");
                                }

                                void AddIfNotEmpty(string label, string? value)
                                {
                                    if (!string.IsNullOrWhiteSpace(value))
                                    {
                                        AddPaymentRow(label, value);
                                    }
                                }

                                AddIfNotEmpty("Bank Name:", bank?.BankName);
                                AddIfNotEmpty("Account Number:", bank?.AccountNumber);
                                AddIfNotEmpty("Sort code:", bank?.SortCode);
                                AddIfNotEmpty("IBAN:", bank?.IBAN);
                                AddIfNotEmpty("SWIFT Code:", bank?.SwiftCode);
                                AddIfNotEmpty("Beneficiary name:", ownerProfile.OwnerName);


                            });

                            col.Item().PaddingTop(10);

                            col.Item().Table(serviceTable =>
                            {
                                serviceTable.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(120);
                                });

                                serviceTable.Header(header =>
                                {
                                    string headerColor = "#1B3942";
                                    header.Cell().Border(0.5f).Background(Color.FromHex(headerColor)).Padding(5)
                                        .Text(text => text.Span("Description").FontColor(Colors.White).Bold());
                                    header.Cell().Border(0.5f).Background(Color.FromHex(headerColor)).Padding(5)
                                        .Text(text => text.Span("Quantity").FontColor(Colors.White).Bold());
                                    header.Cell().Border(0.5f).Background(Color.FromHex(headerColor)).Padding(5)
                                        .Text(text => text.Span($"Rate ({currencyOwnerSymbol})").FontColor(Colors.White).Bold());
                                    header.Cell().Border(0.5f).Background(Color.FromHex(headerColor)).Padding(5)
                                        .Text(text => text.Span($"Subtotal").FontColor(Colors.White).Bold());

                                });

                                foreach (var item in invoiceItems)
                                {
                                    var resource = item.Resource;
                                    decimal originalRate = item.RatePerHour ?? 0m;
                                    decimal originalSubtotal;
                                    decimal subtotal;
                                    string calculation;

                                    if (item.Variation == InvoiceItem.ContractVariation.Hourly)
                                    {
                                        originalSubtotal = (item.ConsumedHours ?? 0m) * (item.RatePerHour ?? 0m);
                                        subtotal = originalSubtotal * conversionRate;

                                        if (conversionRate != 1m)
                                        {
                                            calculation = $"Amount in {currencyOwnerName}: {(item.ConsumedHours ?? 0m)} Hours × {currencyOwnerSymbol}{originalRate:F2} = {currencyOwnerSymbol}{originalSubtotal:F2}\n" +
                                                          $"Converted at 1 {currencyOwnerName} = {conversionRate:F2} {targetCurrencyName}\n" +
                                                          $"Amount in {targetCurrencyName}: {currencyOwnerSymbol}{originalSubtotal:F2} × {conversionRate:F2} = {targetCurrencySymbol}{subtotal:F2}";
                                        }
                                        else
                                        {
                                            calculation = $"Amount in {currencyOwnerName}: {(item.ConsumedHours ?? 0m)} Hours × {currencyOwnerSymbol}{originalRate:F2} = {currencyOwnerSymbol}{subtotal:F2}";
                                        }
                                    }
                                    else
                                    {
                                        originalSubtotal = (item.TotalAmount ?? 0m) / conversionRate;
                                        subtotal = item.TotalAmount ?? 0m;

                                        if (conversionRate != 1m)
                                        {
                                            calculation = $"Fixed Amount in {currencyOwnerName}: {currencyOwnerSymbol}{originalSubtotal:F2}\n" +
                                                          $"Converted at 1 {currencyOwnerName} = {conversionRate:F2} {targetCurrencyName}\n" +
                                                          $"Amount in {targetCurrencyName}: {currencyOwnerSymbol}{originalSubtotal:F2} × {conversionRate:F2} = {targetCurrencySymbol}{subtotal:F2}";
                                        }
                                        else
                                        {
                                            calculation = $"Fixed Amount in {currencyOwnerName}: {currencyOwnerSymbol}{originalSubtotal:F2}";
                                        }
                                    }

                                    totalAmount += subtotal;

                                    serviceTable.Cell().ColumnSpan(4).Border(0.5f).Padding(5)
                                        .Text($"{resource.ResourceName} - {resource.Employee.EmployeeName}({resource.Employee.Designation.DesignationName})  - {item.Variation} Contract - {resource.Recurrence} - {(invoice.DueDate.HasValue ? invoice.DueDate.Value.AddMonths(-1).ToString("MMMM yyyy") : "N/A")}");
                                    serviceTable.Cell().ColumnSpan(1).Border(0.5f).Padding(5)
                                        .Text($"Calculation:\n{calculation}")
                                        .Italic();
                                    serviceTable.Cell().Border(0.5f).Padding(5).AlignCenter()
                                        .Text(item.Variation == InvoiceItem.ContractVariation.Hourly ? (item.ConsumedHours ?? 0m).ToString() : "-");
                                    serviceTable.Cell().Border(0.5f).Padding(5).AlignCenter()
                                        .Text(item.Variation == InvoiceItem.ContractVariation.Hourly ? $"{currencyOwnerSymbol}{originalRate:F2}" : "-");
                                    serviceTable.Cell().Border(0.5f).Padding(5).AlignCenter()
                                       .Text($"{(currencyOwnerSymbol)}{originalSubtotal:F2}");
                                    serviceTable.Cell().ColumnSpan(2).Border(0.5f).Padding(5)
                                        .Text(string.IsNullOrWhiteSpace(item.PurposeCode) ? "Software Consultancy Services" : item.PurposeCode).Bold();
                                }

                                serviceTable.Cell().ColumnSpan(2).Border(0.5f).Table(totalTable =>
                                {
                                    totalTable.ColumnsDefinition(subCols =>
                                    {
                                        subCols.RelativeColumn();
                                        subCols.ConstantColumn(100);
                                    });

                                    totalTable.Cell().Border(0.5f).Padding(5).Text(text => {

                                        if (conversionRate != 1m)
                                        {
                                            text.Span($"Total Amount In ({targetCurrencyName})").Bold();
                                        }
                                        else
                                            text.Span($"Total Amount").Bold();
                                    });
                                    totalTable.Cell().Border(0.5f).Padding(5).AlignRight()
                                        .Text($"{(conversionRate != 1m ? targetCurrencySymbol : currencyOwnerSymbol)}{totalAmount:F2}").Bold();

                                    totalTable.Cell().Border(0.5f).Padding(5).Text("Total Due By").Bold();
                                    totalTable.Cell().Border(0.5f).Padding(5).AlignRight()
                                        .Text($"{(invoice.DueDate.HasValue ? invoice.DueDate.Value.ToString("MM/dd/yyyy") : "N/A")}").Bold();
                                });
                            });

                            col.Item().PaddingTop(5);

                            col.Item().PaddingTop(5)
                                              .Text($"Payment Communication: {invoice.Payment_Communication ?? "N/A"}")
                                              .FontSize(12)
                                              .FontColor(Color.FromHex(headerColor))
                                              .Bold();

                            col.Item().PaddingTop(5);

                            if (invoice.PaymentGuidelineImage != null && invoice.PaymentGuidelineImage.Length > 0 && !receipt)
                            {
                                col.Item().PaddingTop(5).Text("Transfer Instructions").FontSize(12).FontColor(Color.FromHex(headerColor)).Bold();
                                col.Item().PaddingTop(25).Container().MaxHeight(400).MaxWidth(500)
                                    .Image(invoice.PaymentGuidelineImage, ImageScaling.FitArea);
                            }

                            // Add payment confirmation section if receipt is true
                            if (receipt)
                            {
                                col.Item().PaddingTop(20);
                                col.Item().Text("Payment Confirmation").FontSize(12).FontColor(Color.FromHex(headerColor)).Bold();
                                col.Item().Table(paymentTable =>
                                {
                                    paymentTable.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    paymentTable.Cell().Padding(5).Text("Payment Date:").Bold();
                                    paymentTable.Cell().Padding(5).Text(DateTime.Now.ToString("MM/dd/yyyy"));

                                    paymentTable.Cell().Padding(5).Text("Payment Method:").Bold();
                                    paymentTable.Cell().Padding(5).Text("Bank Transfer");

                                    paymentTable.Cell().Padding(5).Text("Amount Received:").Bold();
                                    paymentTable.Cell().Padding(5).Text($"{(conversionRate != 1m ? targetCurrencySymbol : currencyOwnerSymbol)}{totalAmount:F2}").Bold();
                                    int invoiceNumber = client.InvoiceSeriesStart + invoiceId;
                                    string paddedInvoiceNumber = invoiceNumber.ToString("D6");
                                    paymentTable.Cell().Padding(5).Text("Invoice Id:").Bold();
                                    paymentTable.Cell().Padding(5).Text($"{"INV"}/{invoice.InvoiceDate.Year}/{paddedInvoiceNumber}");

                                });
                            }
                        });

                        page.Footer().AlignCenter().Text("Email: suleman@atrule.com | Web: atrule.com | Phone: +92-313-6120356").FontSize(10);
                    });
                }).GeneratePdf(ms);

                // Update invoice totals
                invoice.TotalAmount = totalAmount;
                invoice.RemainingAmount = receipt ? 0m : totalAmount - (invoice.PaidAmount ?? 0m);
                await context.SaveChangesAsync();
                _logger.LogInformation($"Generated PDF for Invoice {invoiceId} with TotalAmount: {totalAmount}.");

                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating invoice PDF for Invoice {invoiceId}.");
                throw;
            }
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var invoices = await context.Invoices
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.Client)
                        .ThenInclude(c => c.CountryCurrency)
                    .Include(i => i.InvoiceItems)
                        .ThenInclude(ii => ii.Resource)
                            .ThenInclude(r => r.Employee)
                    .Include(i => i.InvoiceItems)
                        .ThenInclude(ii => ii.Resource)
                            .ThenInclude(r => r.OwnerProfile)
                                .ThenInclude(op => op.CountryCurrency)
                    .Include(i => i.CountryCurrency)
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {invoices.Count} invoices.");
                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all invoices.");
                throw;
            }
        }

        public async Task<bool> SendInvoiceToClientAsync(
           int invoiceId,
           string customTemplate = null,
           bool includeInvoice = true,
           List<byte[]> additionalAttachments = null,
           bool receipt = false)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.Client)
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

                if (invoice == null)
                {
                    _logger.LogError($"Invoice with ID {invoiceId} not found.");
                    throw new Exception("Invoice not found!");
                }

                var attachments = new List<byte[]>();
                if (includeInvoice)
                {
                    byte[] pdfData = await GenerateInvoicePdfAsync(invoiceId, receipt: receipt);
                    if (pdfData != null)
                    {
                        attachments.Add(pdfData);
                        _logger.LogInformation($"{(receipt ? "Receipt" : "Invoice")} PDF added to attachments for Invoice {invoiceId}.");
                    }
                }
                else
                {
                    _logger.LogInformation($"{(receipt ? "Receipt" : "Invoice")} PDF not included for Invoice {invoiceId} as per toggle state.");
                }

                if (additionalAttachments != null && additionalAttachments.Any())
                {
                    attachments.AddRange(additionalAttachments);
                    _logger.LogInformation($"Added {additionalAttachments.Count} additional attachments for Invoice {invoiceId}.");
                }

                string defaultFileName = receipt
                    ? $"Receipt_{invoice.InvoiceId}.pdf"
                    : $"Invoice_{invoice.InvoiceId}.pdf";

                bool emailSent = await _emailService.SendInvoiceEmailAsync(
                    invoice.Client.Email,
                    attachments,
                    defaultFileName,
                    invoice.Client.Name,
                    invoice.InvoiceId,
                    invoice.DueDate ?? DateTime.Now.AddDays(5),
                    invoice.Client.InvoiceSeriesStart,
                    customTemplate,
                    receipt);

                if (emailSent)
                {
                    invoice.EmailStatus = "Sent";
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Email sent successfully for {(receipt ? "Receipt" : "Invoice")} {invoiceId}.");
                }
                else
                {
                    _logger.LogWarning($"Failed to send email for {(receipt ? "Receipt" : "Invoice")} {invoiceId}.");
                }

                return emailSent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending {(receipt ? "receipt" : "invoice")} email for Invoice {invoiceId}.");
                throw;
            }
        }

        public async Task MarkInvoiceAsPaidAsync(int invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Where(i => !i.IsDeleted)
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
                if (invoice == null)
                {
                    _logger.LogError($"Invoice with ID {invoiceId} not found.");
                    throw new Exception("Invoice not found!");
                }

                invoice.InvoiceStatuses = InvoiceStatus.Paid;
                invoice.PaidAmount = invoice.TotalAmount; // Set PaidAmount to TotalAmount
                invoice.RemainingAmount = 0m; // RemainingAmount should be 0
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Invoice {invoiceId} marked as paid.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking Invoice {invoiceId} as paid.");
                throw;
            }
        }

        public async Task DeleteInvoiceAsync(int invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
                if (invoice == null)
                {
                    _logger.LogWarning($"Invoice with ID {invoiceId} not found for deletion.");
                    return;
                }

                invoice.IsDeleted = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Invoice {invoiceId} deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting Invoice {invoiceId}.");
                throw;
            }
        }

        public async Task<Invoice> GetInvoiceById(int invoiceId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var invoice = await context.Invoices
                   .Where(i => !i.IsDeleted)
                   .Include(i => i.Client)
                       .ThenInclude(c => c.CountryCurrency)
                   .Include(i => i.InvoiceItems)
                       .ThenInclude(ii => ii.Resource)
                           .ThenInclude(r => r.Employee)
                   .Include(i => i.InvoiceItems)
                       .ThenInclude(ii => ii.Resource)
                           .ThenInclude(r => r.OwnerProfile)
                               .ThenInclude(op => op.CountryCurrency)
                   .Include(i => i.CountryCurrency)
                   .IgnoreQueryFilters()
                   .AsNoTracking()
                   .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

                if (invoice != null)
                    return invoice;
                return new Invoice();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> UpdateInvoiceAsync(
      int invoiceId,
      int clientId,
      int resourceId,
      decimal consumedHours,
      decimal rate,
      decimal amount,
      InvoiceItem.ContractVariation variation,
      DateTime? startDate,
      DateTime? endDate,
      DateTime? dueDate,
      string purposeCode,
      decimal conversionRate,
      byte[] paymentGuidelineImage,
      int? ownerCurrencyId,
      int bankAccountId,
      string Payment_Communication,
      int selectedOwnerProfileId)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

                if (invoice == null)
                {
                    _logger.LogError($"Invoice with ID {invoiceId} not found.");
                    return false;
                }

                var resourse = await _context.Resources.Include(r => r.OwnerProfile).FirstOrDefaultAsync(r => r.ResourceId == resourceId);
                resourse.OwnerProfileId = selectedOwnerProfileId;
                var owner = _context.Owners.FirstOrDefault(o => o.Id == resourse.OwnerProfileId);

                int? invoiceCurrencyId = ownerCurrencyId;
                if (!invoiceCurrencyId.HasValue)
                {
                    invoiceCurrencyId = owner?.CountryCurrencyId;
                }

                    // Update invoice fields
                    invoice.ClientId = clientId;
                invoice.StartDate = startDate;
                invoice.EndDate = endDate;
                invoice.DueDate = dueDate;
                invoice.TargetCurrencyConversionRate = conversionRate;
                invoice.PaymentGuidelineImage = paymentGuidelineImage;
                invoice.CountryCurrencyId = invoiceCurrencyId ?? invoice.CountryCurrencyId;
                invoice.BankAccountId = bankAccountId;
                invoice.Payment_Communication = Payment_Communication;

                // Handle invoice item
                var item = invoice.InvoiceItems.FirstOrDefault();
                if (item == null)
                {
                    item = new InvoiceItem
                    {
                        InvoiceId = invoiceId,
                        ResourceId = resourceId,
                        Variation = variation,
                        PurposeCode = purposeCode,
                        ConsumedHours = variation == InvoiceItem.ContractVariation.Hourly ? consumedHours : 0,
                        RatePerHour = variation == InvoiceItem.ContractVariation.Hourly ? rate : 0,
                        TotalAmount = variation == InvoiceItem.ContractVariation.Hourly
                            ? (consumedHours * rate) * conversionRate
                            : amount * conversionRate
                    };
                    _context.InvoiceItems.Add(item);
                }
                else
                {
                    item.ResourceId = resourceId;
                    item.Variation = variation;
                    item.PurposeCode = purposeCode;
                    item.ConsumedHours = variation == InvoiceItem.ContractVariation.Hourly ? consumedHours : 0;
                    item.RatePerHour = variation == InvoiceItem.ContractVariation.Hourly ? rate : 0;
                    item.TotalAmount = variation == InvoiceItem.ContractVariation.Hourly
                        ? (consumedHours * rate) * conversionRate
                        : amount * conversionRate;
                }

                // Recalculate invoice totals
                invoice.TotalAmount = invoice.InvoiceItems.Sum(i => i.TotalAmount ?? 0);
                invoice.RemainingAmount = invoice.TotalAmount - (invoice.PaidAmount ?? 0);

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Invoice {invoiceId} updated successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating invoice {invoiceId}");
                return false;
            }
        }

    }
}



// Overload for single resource
//public async Task<int> SaveInvoiceAsync(int clientId, int resourceId, decimal consumedHours, decimal rate, decimal amount, ContractVariation variation, DateTime? startDate, DateTime? endDate, DateTime? dueDate, string purposeCode, decimal conversionRate = 1m)
//{
//    decimal totalAmount = variation == ContractVariation.Hourly ? consumedHours * rate : amount;
//    totalAmount *= conversionRate;

//    var invoiceItem = new InvoiceItem
//    {
//        ResourceId = resourceId,
//        ConsumedHours = variation == ContractVariation.Hourly ? consumedHours : 0,
//        RatePerHour = rate,
//        PurposeCode = purposeCode,
//        TotalAmount = totalAmount
//    };

//    var invoiceItems = new List<InvoiceItem> { invoiceItem };

//    return await SaveInvoiceAsync(clientId, invoiceItems, startDate, endDate, dueDate, conversionRate);
//}

//public async Task<List<int>> SaveInvoicesAsync(int clientId, List<InvoiceItem> invoiceItems, DateTime? startDate, DateTime? endDate, DateTime? dueDate, decimal conversionRate = 1m)
//{
//    var invoiceIds = new List<int>();

//    using var transaction = await _context.Database.BeginTransactionAsync();
//    try
//    {
//        var client = await _context.Clients
//            .Include(c => c.CountryCurrency)
//            .FirstOrDefaultAsync(c => c.ClientId == clientId);
//        if (client == null)
//            throw new Exception("Client not found!");

//        foreach (var item in invoiceItems)
//        {
//            var resource = await _context.Resources
//                .Include(r => r.Employee)
//                .FirstOrDefaultAsync(r => r.ResourceId == item.ResourceId && r.ClientId == clientId);
//            if (resource == null)
//                throw new Exception($"Resource {item.ResourceId} not found!");

//            if (resource.Variation == ContractVariation.Hourly && item.ConsumedHours > resource.CommittedHours)
//            {
//                Console.WriteLine($"⚠️ Warning: Consumed hours ({item.ConsumedHours}) exceed committed hours ({resource.CommittedHours}) for Resource {item.ResourceId}.");
//            }

//            var newInvoice = new Invoice
//            {
//                ClientId = clientId,
//                InvoiceDate = endDate?.Date ?? DateTime.UtcNow,
//                StartDate = startDate,
//                EndDate = endDate,
//                DueDate = dueDate,
//                TotalAmount = item.TotalAmount,
//                CountryCurrencyId = client.CountryCurrencyId,
//                InvoiceStatuses = InvoiceStatus.Pending,
//                EmailStatus = "Not Sent",
//                InvoiceItems = new List<InvoiceItem> { item },
//                TargetCurrencyConversionRate = conversionRate
//            };

//            await _context.Invoices.AddAsync(newInvoice);
//            await _context.SaveChangesAsync();

//            invoiceIds.Add(newInvoice.InvoiceId);
//        }

//        await transaction.CommitAsync();
//        return invoiceIds;
//    }
//    catch (Exception ex)
//    {
//        await transaction.RollbackAsync();
//        Console.WriteLine($"❌ Error saving invoices: {ex.Message}");
//        throw;
//    }
//}

//private async Task<int> SaveInvoiceAsync(int clientId, List<InvoiceItem> invoiceItems, DateTime? startDate, DateTime? endDate, DateTime? dueDate, decimal conversionRate = 1m)
//{
//    using var transaction = await _context.Database.BeginTransactionAsync();
//    try
//    {
//        var client = await _context.Clients
//            .Include(c => c.CountryCurrency)
//            .FirstOrDefaultAsync(c => c.ClientId == clientId);
//        if (client == null)
//            throw new Exception("Client not found!");

//        foreach (var item in invoiceItems)
//        {
//            var resource = await _context.Resources
//                .Include(r => r.Employee)
//                .FirstOrDefaultAsync(r => r.ResourceId == item.ResourceId && r.ClientId == clientId);
//            if (resource == null)
//                throw new Exception($"Resource {item.ResourceId} not found!");

//            if (resource.Variation == ContractVariation.Hourly && item.ConsumedHours > resource.CommittedHours)
//            {
//                Console.WriteLine($"⚠️ Warning: Consumed hours ({item.ConsumedHours}) exceed committed hours ({resource.CommittedHours}) for Resource {item.ResourceId}.");
//            }
//        }

//        decimal totalAmount = invoiceItems.Sum(item => item.TotalAmount);

//        var newInvoice = new Invoice
//        {
//            ClientId = clientId,
//            InvoiceDate = endDate?.Date ?? DateTime.UtcNow,
//            StartDate = startDate,
//            EndDate = endDate,
//            DueDate = dueDate,
//            TotalAmount = totalAmount,
//            CountryCurrencyId = client.CountryCurrencyId,
//            InvoiceStatuses = InvoiceStatus.Pending,
//            EmailStatus = "Not Sent",
//            InvoiceItems = invoiceItems,
//            TargetCurrencyConversionRate = conversionRate
//        };

//        await _context.Invoices.AddAsync(newInvoice);
//        await _context.SaveChangesAsync();

//        await transaction.CommitAsync();
//        return newInvoice.InvoiceId;
//    }
//    catch (Exception ex)
//    {
//        await transaction.RollbackAsync();
//        Console.WriteLine($"❌ Error saving invoice: {ex.Message}");
//        throw;
//    }
//}
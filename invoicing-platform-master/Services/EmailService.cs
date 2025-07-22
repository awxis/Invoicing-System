using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Client_Invoice_System.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly string _logoPath;

        public EmailService(IConfiguration config)
        {
            _config = config;
            _logoPath = @"C:\Workspace\Invoicing_platform\wwwroot\images\logo.png";
        }

        public async Task<bool> SendInvoiceEmailAsync(string recipientEmail, List<byte[]> attachments, string defaultFileName, string clientName, int invoiceId, DateTime dueDate, int invoiceSeriesStart, string customTemplate = null, bool receipt = false)
        {
            try
            {
                var emailSettings = _config.GetSection("EmailSettings");

                if (emailSettings == null)
                    throw new Exception("Email settings not found in configuration.");

                string senderEmail = emailSettings["SenderEmail"];
                string smtpServer = emailSettings["SmtpServer"];
                string smtpPort = emailSettings["SmtpPort"];
                string senderPassword = emailSettings["SenderPassword"];

                if (new[] { senderEmail, smtpServer, smtpPort, senderPassword }.Any(string.IsNullOrEmpty))
                    throw new Exception("Missing SMTP configuration details.");

                int invoiceNumber = invoiceSeriesStart + invoiceId;
                string paddedInvoiceNumber = invoiceNumber.ToString("D6");
                string formattedInvoiceNumber = $"INV/{dueDate.Year}/{paddedInvoiceNumber}";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Atrule Technologies Invoicing Updates", senderEmail));
                message.To.Add(new MailboxAddress("", recipientEmail));
                message.Subject = receipt ? $"Receipt #{invoiceId} from Atrule Technologies" : $"Invoice #{invoiceId} from Atrule Technologies";

                var builder = new BodyBuilder();

                bool hasAttachments = attachments != null && attachments.Any();
                if (hasAttachments && File.Exists(_logoPath))
                {
                    Console.WriteLine($"Logo found at: {_logoPath}");
                    using (var logoStream = new FileStream(_logoPath, FileMode.Open, FileAccess.Read))
                    {
                        var logo = builder.LinkedResources.Add("logo.png", logoStream, new ContentType("image", "png"));
                        logo.ContentId = "company-logo";
                    }
                }
                else
                {
                    Console.WriteLine("No attachments or logo not found, skipping logo embedding.");
                }

                string emailBody;
                if (!string.IsNullOrEmpty(customTemplate))
                {
                    emailBody = WrapCustomTemplate(customTemplate, clientName, formattedInvoiceNumber, dueDate, hasAttachments);
                }
                else
                {
                    emailBody = GenerateEmailBody(clientName, formattedInvoiceNumber, dueDate, hasAttachments);
                }

                builder.HtmlBody = emailBody;
                builder.TextBody = GeneratePlainTextBody(clientName, formattedInvoiceNumber, dueDate, receipt);

                if (hasAttachments)
                {
                    for (int i = 0; i < attachments.Count; i++)
                    {
                        string fileName = i == 0 ? defaultFileName : $"Attachment_{i}.{DateTime.Now:yyyyMMddHHmmss}.pdf";
                        builder.Attachments.Add(fileName, attachments[i], new ContentType("application", "pdf"));
                    }
                }

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, int.Parse(smtpPort), MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, senderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"✅ Email sent successfully to {recipientEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Email Sending Error: {ex.Message}");
                return false;
            }
        }

        private string GenerateEmailBody(string clientName, string formattedInvoiceNumber, DateTime dueDate, bool hasAttachments, bool receipt = false)
        {
            string documentType = receipt ? "receipt" : "invoice";
            string documentLabel = receipt ? "Receipt" : "Invoice";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; margin: 20px auto; background-color: #ffffff; border: 1px solid #e0e0e0;"">
        <tr>
            <td style=""padding: 20px; background-color: #2f4f4f;"">
                <table cellpadding=""0"" cellspacing=""0"" style=""text-align: left;"">
                    <tr>
                        <td style=""vertical-align: middle; padding: 0;"">
                            <img src=""cid:company-logo"" alt=""Atrule Technologies LLC"" style=""max-width: 50px; display: block;"" />
                        </td>
                        <td style=""vertical-align: middle; padding: 0;"">
                            <span style=""color: #ffffff; font-size: 20px; font-weight: bold; display: inline-block; margin-left: 5px;"">
                                Atrule Technologies LLC
                            </span>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
        <tr>
            <td style=""padding: 20px;"">
                <h2 style=""color: #333333; margin: 0 0 15px; font-size: 22px;"">Dear {(string.IsNullOrEmpty(clientName) ? "Valued Client" : clientName)},</h2>
                <p style=""color: #555555; line-height: 1.8; margin: 0 0 15px; font-size: 16px;"">
                    Thank you for your business with Atrule Technologies. {(hasAttachments ? "We have attached the relevant documents for your review." : "Your invoice details are below.")}
                </p>
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 15px 0;"">
                    <tr>
                        <td style=""padding: 10px 0; border-bottom: 1px solid #e0e0e0;"">
                            <strong style=""color: #333333; font-size: 16px;"">{documentLabel} Number:</strong>
                            <span style=""color: #555555; font-size: 16px; margin-left: 10px;"">{formattedInvoiceNumber}</span>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 10px 0; border-bottom: 1px solid #e0e0e0;"">
                            <strong style=""color: #333333; font-size: 16px;"">Due Date:</strong>
                            <span style=""color: #555555; font-size: 16px; margin-left: 10px;"">{dueDate:MMMM dd, yyyy}</span>
                        </td>
                    </tr>
                </table>
                {(hasAttachments ? $@"
                <p style=""color: #555555; line-height: 1.8; margin: 15px 0; font-size: 16px;"">
                    Please ensure payment is made by the due date to avoid any late fees. You can find the attached files below for your reference.
                </p>
                <table width=""100%"" cellpadding=""10"" cellspacing=""0"" style=""margin: 20px 0; background-color: #f9f9f9; border: 1px solid #e0e0e0;"">
                    <tr>
                        <td style=""text-align: center;"">
                            <span style=""display: inline-block; padding: 10px 20px; background-color: #2f4f4f; color: #ffffff; border-radius: 5px; font-size: 14px;"">
                                📎 Files Attached Below
                            </span>
                        </td>
                    </tr>
                </table>" : $@"
                <p style=""color: #555555; line-height: 1.8; margin: 15px 0; font-size: 16px;"">
                    Please find the {documentType} attached as a PDF. {(receipt ? "" : "Ensure payment is made by the due date to avoid any late fees.")}
                </p>
                <table width=""100%"" cellpadding=""10"" cellspacing=""0"" style=""margin: 20px 0; background-color: #f9f9f9; border: 1px solid #e0e0e0;"">
                    <tr>
                        <td style=""text-align: center;"">
                            <a href=""#"" style=""display: inline-block; padding: 10px 20px; background-color: #2f4f4f; color: #ffffff; text-decoration: none; border-radius: 5px;"">View {documentLabel} PDF</a>
                        </td>
                    </tr>
                </table>")}
                <p style=""color: #555555; line-height: 1.8; margin: 15px 0; font-size: 16px;"">
                    If you have any questions or need further assistance, please feel free to reach out to us at 
                    <a href=""mailto:suleman@atrule.com"" style=""color: #2f4f4f; text-decoration: underline;"">suleman@atrule.com</a> 
                    or call us at <a href=""tel:+923136120356"" style=""color: #2f4f4f; text-decoration: none;"">+92-313-6120356</a>.
                </p>
                <p style=""color: #555555; line-height: 1.8; margin: 15px 0; font-size: 16px;"">
                    Warm regards,<br>
                    The Atrule Technologies Team
                </p>
            </td>
        </tr>
        <tr>
            <td style=""padding: 15px; text-align: center; background-color: #2f4f4f; color: #ffffff; font-size: 12px; line-height: 1.6;"">
                Atrule Technologies | 2nd Floor, Khawar Center, SP Chowk, Multan, Pakistan<br>
                Email: <a href=""mailto:suleman@atrule.com"" style=""color: #ffffff; text-decoration: none;"">suleman@atrule.com</a> | 
                Web: <a href=""https://atrule.com"" style=""color: #ffffff; text-decoration: none;"">atrule.com</a> | 
                Phone: <a href=""tel:+923136120356"" style=""color: #ffffff; text-decoration: none;"">+92-313-6120356</a>
            </td>
        </tr>
    </table>
</body>
</html>";
        }


        private string WrapCustomTemplate(string customTemplate, string clientName, string formattedInvoiceNumber, DateTime dueDate, bool hasAttachments)
        {
            string formattedCustomBody = customTemplate
                .Replace("{ClientName}", clientName ?? "Valued Client")
                .Replace("{InvoiceNumber}", formattedInvoiceNumber)
                .Replace("{DueDate}", dueDate.ToString("MMMM dd, yyyy"))
                .Replace("\n", "<br>");

            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; margin: 20px auto; background-color: #ffffff; border: 1px solid #e0e0e0;"">
        <tr>
            <td style=""padding: 20px; background-color: #2f4f4f;"">
                <table cellpadding=""0"" cellspacing=""0"" style=""text-align: left;"">
                    <tr>
                        <td style=""vertical-align: middle; padding: 0;"">
                            <img src=""cid:company-logo"" alt=""Atrule Technologies LLC"" style=""max-width: 50px; display: block;"" />
                        </td>
                        <td style=""vertical-align: middle; padding: 0;"">
                            <span style=""color: #ffffff; font-size: 20px; font-weight: bold; display: inline-block; margin-left: 5px;"">
                                Atrule Technologies LLC
                            </span>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
        <tr>
            <td style=""padding: 20px;"">
                " + formattedCustomBody + @"
                " + (hasAttachments ? @"
                <table width=""100%"" cellpadding=""10"" cellspacing=""0"" style=""margin: 20px 0; background-color: #f9f9f9; border: 1px solid #e0e0e0;"">
                    <tr>
                        <td style=""text-align: center;"">
                            <span style=""display: inline-block; padding: 10px 20px; background-color: #2f4f4f; color: #ffffff; border-radius: 5px; font-size: 14px;"">
                                📎 Files Attached Below
                            </span>
                        </td>
                    </tr>
                </table>" : "") + @"
                <p style=""color: #555555; line-height: 1.8; margin: 15px 0; font-size: 16px;"">
                    If you have any questions or need further assistance, please feel free to reach out to us at 
                    <a href=""mailto:suleman@atrule.com"" style=""color: #2f4f4f; text-decoration: underline;"">suleman@atrule.com</a> 
                    or call us at <a href=""tel:+923136120356"" style=""color: #2f4f4f; text-decoration: none;"">+92-313-6120356</a>.
                </p>
                <p style=""color: #555555; line-height: 1.8; margin: 15px 0; font-size: 16px;"">
                    Warm regards,<br>
                    The Atrule Technologies Team
                </p>
            </td>
        </tr>
        <tr>
            <td style=""padding: 15px; text-align: center; background-color: #2f4f4f; color: #ffffff; font-size: 12px; line-height: 1.6;"">
                Atrule Technologies | 2nd Floor, Khawar Center, SP Chowk, Multan, Pakistan<br>
                Email: <a href=""mailto:suleman@atrule.com"" style=""color: #ffffff; text-decoration: none;"">suleman@atrule.com</a> | 
                Web: <a href=""https://atrule.com"" style=""color: #ffffff; text-decoration: none;"">atrule.com</a> | 
                Phone: <a href=""tel:+923136120356"" style=""color: #ffffff; text-decoration: none;"">+92-313-6120356</a>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private string GeneratePlainTextBody(string clientName, string formattedInvoiceNumber, DateTime dueDate, bool receipt = false)
        {
            string documentType = receipt ? "receipt" : "invoice";
            string documentLabel = receipt ? "Receipt" : "Invoice";
            string lateFeeNotice = receipt ? "" : "\nPlease ensure payment is made by the due date to avoid any late fees.";

            return $@"Dear {(string.IsNullOrEmpty(clientName) ? "Valued Client" : clientName)},

Thank you for {(receipt ? "your business with" : "choosing")} Atrule Technologies. {(receipt ? "Please find your" : "Your")} {documentType} details {(receipt ? $"({documentLabel} #{formattedInvoiceNumber}) attached" : "are below")}.

{documentLabel} Number: {formattedInvoiceNumber}
Due Date: {dueDate:MMMM dd, yyyy}{lateFeeNotice}

For any questions or assistance, contact us at suleman@atrule.com or +92-313-6120356.

{(receipt ? "Best regards" : "Warm regards")},
The Atrule Technologies Team

---
Atrule Technologies
2nd Floor, Khawar Center, SP Chowk, Multan, Pakistan
Email: suleman@atrule.com | Web: atrule.com | Phone: +92-313-6120356";
        }
    }
}
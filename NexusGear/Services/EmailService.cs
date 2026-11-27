using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NexusGear.Models;
using NexusGear.Services.Interfaces;

namespace NexusGear.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOrderConfirmationEmailAsync(Order order)
    {
        if (order == null || order.User == null)
        {
            _logger.LogWarning("Cannot send confirmation email: Order or User is null.");
            return;
        }

        string recipientEmail = order.User.Email;
        string recipientName = order.User.FullName;

        var recipientOverride = _configuration["Smtp:RecipientOverride"] ?? _configuration["Smtp__RecipientOverride"];
        if (!string.IsNullOrWhiteSpace(recipientOverride))
        {
            recipientEmail = recipientOverride;
        }

        string subject = $"Order Confirmed — {order.OrderNumber} (NexusGear)";
        string htmlBody = GenerateHtmlReceipt(order);

        var host = _configuration["Smtp:Host"] ?? _configuration["Smtp__Host"] ?? "sandbox.smtp.mailtrap.io";
        var portStr = _configuration["Smtp:Port"] ?? _configuration["Smtp__Port"] ?? "2525";
        var username = _configuration["Smtp:Username"] ?? _configuration["Smtp__Username"] ?? string.Empty;
        var password = _configuration["Smtp:Password"] ?? _configuration["Smtp__Password"] ?? string.Empty;
        var fromEmail = _configuration["Smtp:FromEmail"] ?? _configuration["Smtp__FromEmail"] ?? "noreply@nexusgear.ro";
        var fromName = _configuration["Smtp:FromName"] ?? _configuration["Smtp__FromName"] ?? "NexusGear Orders";

        int.TryParse(portStr, out int port);
        if (port <= 0) port = 2525;

        bool isPlaceholder = string.IsNullOrWhiteSpace(username) ||
                             string.IsNullOrWhiteSpace(password) ||
                             username.Contains("placeholder", StringComparison.OrdinalIgnoreCase);

        if (isPlaceholder)
        {
            _logger.LogInformation(
                "[MAILTRAP SIMULATOR] Transactional email receipt logged for {Email}. Order: {OrderNumber}, Total: {Total:N2} RON",
                recipientEmail, order.OrderNumber, order.TotalAmount);
            return;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true,
                Timeout = 10000
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8
            };
            message.To.Add(new MailAddress(recipientEmail, recipientName));

            await client.SendMailAsync(message);
            _logger.LogInformation("Order confirmation email successfully dispatched via Mailtrap to {Email}", recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order confirmation email via Mailtrap SMTP to {Email}", recipientEmail);
        }
    }

    private static string GenerateHtmlReceipt(Order order)
    {
        var customerName = WebUtility.HtmlEncode(order.User?.FullName ?? "Valued Customer");
        var orderNumber = WebUtility.HtmlEncode(order.OrderNumber);
        var street = WebUtility.HtmlEncode(order.Address?.Street ?? string.Empty);
        var city = WebUtility.HtmlEncode(order.Address?.City ?? string.Empty);
        var county = WebUtility.HtmlEncode(order.Address?.County ?? string.Empty);
        var postalCode = WebUtility.HtmlEncode(order.Address?.PostalCode ?? string.Empty);
        var shippingMethod = WebUtility.HtmlEncode(order.ShippingMethod);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: #0B1120; color: #FFFFFF; margin: 0; padding: 24px; }");
        sb.AppendLine(".card { max-width: 600px; margin: 0 auto; background-color: #131D31; border: 1px solid #1E293B; border-radius: 12px; padding: 28px; }");
        sb.AppendLine(".header { text-align: center; border-bottom: 1px solid #1E293B; padding-bottom: 20px; margin-bottom: 20px; }");
        sb.AppendLine(".title { font-size: 24px; font-weight: bold; color: #FFFFFF; margin: 0 0 6px 0; }");
        sb.AppendLine(".order-tag { font-family: monospace; font-size: 16px; color: #00E5A3; background-color: #0B1120; padding: 6px 14px; border-radius: 6px; display: inline-block; font-weight: bold; border: 1px solid #1E293B; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 16px; color: #FFFFFF; }");
        sb.AppendLine("th { text-align: left; color: #94A3B8; font-size: 12px; text-transform: uppercase; padding: 10px 0; border-bottom: 1px solid #1E293B; }");
        sb.AppendLine("td { padding: 14px 0; border-bottom: 1px solid #1E293B; font-size: 14px; }");
        sb.AppendLine(".total-row { font-size: 18px; font-weight: bold; color: #00E5A3; }");
        sb.AppendLine(".footer { text-align: center; color: #94A3B8; font-size: 12px; margin-top: 24px; line-height: 1.6; }");
        sb.AppendLine("</style></head><body style='background-color: #0B1120; color: #FFFFFF; margin: 0; padding: 24px; font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif;'>");

        sb.AppendLine("<div class='card' style='max-width: 600px; margin: 0 auto; background-color: #131D31; border: 1px solid #1E293B; border-radius: 12px; padding: 28px;'>");
        sb.AppendLine("<div class='header' style='text-align: center; border-bottom: 1px solid #1E293B; padding-bottom: 20px; margin-bottom: 20px;'>");
        sb.AppendLine("<h1 class='title' style='font-size: 24px; font-weight: bold; color: #FFFFFF; margin: 0 0 8px 0;'>⚡ NexusGear — Order Confirmed</h1>");
        sb.AppendLine($"<p style='color: #94A3B8; margin: 0 0 14px 0; font-size: 15px;'>Thank you for your purchase, <strong style='color: #FFFFFF;'>{customerName}</strong>!</p>");
        sb.AppendLine($"<div class='order-tag' style='font-family: monospace; font-size: 16px; color: #00E5A3; background-color: #0B1120; padding: 6px 14px; border-radius: 6px; display: inline-block; font-weight: bold; border: 1px solid #1E293B;'>{orderNumber}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div style='margin-bottom: 20px; background-color: #0B1120; border: 1px solid #1E293B; border-radius: 8px; padding: 14px 18px;'>");
        sb.AppendLine("<p style='margin: 0 0 6px 0; color: #00E5A3; font-size: 11px; font-weight: bold; letter-spacing: 0.5px;'>SHIPPING DESTINATION</p>");
        sb.AppendLine($"<p style='margin: 0; font-size: 14px; color: #FFFFFF; line-height: 1.5;'><strong style='color: #FFFFFF;'>{street}</strong>, {city}, jud. {county} ({postalCode})<br><span style='color: #94A3B8;'>Courier Service:</span> <span style='color: #00E5A3; font-weight: 500;'>{shippingMethod}</span></p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<table style='width: 100%; border-collapse: collapse; margin-top: 16px; color: #FFFFFF;'>");
        sb.AppendLine("<thead><tr><th style='text-align: left; color: #94A3B8; font-size: 12px; text-transform: uppercase; padding: 10px 0; border-bottom: 1px solid #1E293B;'>Product</th><th style='text-align:center; color: #94A3B8; font-size: 12px; text-transform: uppercase; padding: 10px 0; border-bottom: 1px solid #1E293B;'>Qty</th><th style='text-align:right; color: #94A3B8; font-size: 12px; text-transform: uppercase; padding: 10px 0; border-bottom: 1px solid #1E293B;'>Price</th></tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var item in order.OrderItems)
        {
            var lineTotal = item.Quantity * item.UnitPrice;
            var productName = WebUtility.HtmlEncode(item.Product?.Name ?? "Hardware Item");
            sb.AppendLine($"<tr><td style='padding: 14px 0; border-bottom: 1px solid #1E293B; font-size: 14px;'><strong style='color: #FFFFFF !important; font-size: 14px;'>{productName}</strong></td><td style='text-align:center; padding: 14px 0; border-bottom: 1px solid #1E293B; font-size: 14px;'><span style='color: #E2E8F0 !important;'>{item.Quantity}</span></td><td style='text-align:right; padding: 14px 0; border-bottom: 1px solid #1E293B; font-size: 14px;'><strong style='color: #FFFFFF !important;'>{lineTotal:N2} RON</strong></td></tr>");
        }
        sb.AppendLine("</tbody>");
        sb.AppendLine("<tfoot>");
        sb.AppendLine($"<tr><td colspan='2' style='text-align:right; padding: 10px 0; font-size: 14px; border-bottom: 1px solid #1E293B;'><span style='color: #60A5FA !important; font-weight: 600;'>Delivery Fee:</span></td><td style='text-align:right; padding: 10px 0; font-size: 14px; border-bottom: 1px solid #1E293B;'>{(order.ShippingCost == 0 ? "<span style='color: #00E5A3 !important; font-weight: bold;'>FREE</span>" : $"<span style='color: #60A5FA !important; font-weight: bold;'>{order.ShippingCost:N2} RON</span>")}</td></tr>");
        if (order.DiscountAmount > 0)
        {
            sb.AppendLine($"<tr><td colspan='2' style='text-align:right; padding: 10px 0; font-size: 14px; border-bottom: 1px solid #1E293B;'><span style='color: #FBBF24 !important; font-weight: 600;'>Savings &amp; Discount:</span></td><td style='text-align:right; padding: 10px 0; font-size: 14px; border-bottom: 1px solid #1E293B;'><span style='color: #FBBF24 !important; font-weight: bold; font-size: 15px;'>-{order.DiscountAmount:N2} RON</span></td></tr>");
        }
        sb.AppendLine($"<tr class='total-row'><td colspan='2' style='text-align:right; padding: 16px 0; border-top: 2px solid #00E5A3;'><span style='color: #00E5A3 !important; font-size: 18px; font-weight: bold; letter-spacing: 0.3px;'>Total Paid:</span></td><td style='text-align:right; padding: 16px 0; border-top: 2px solid #00E5A3;'><span style='color: #00E5A3 !important; font-size: 20px; font-weight: bold; text-shadow: 0 0 10px rgba(0,229,163,0.3);'>{order.TotalAmount:N2} RON</span></td></tr>");
        sb.AppendLine("</tfoot></table>");

        sb.AppendLine("<div class='footer' style='text-align: center; color: #94A3B8; font-size: 12px; margin-top: 26px; line-height: 1.6; border-top: 1px solid #1E293B; padding-top: 18px;'>");
        sb.AppendLine("<p style='margin: 0;'>This email serves as your official electronic purchase receipt.<br><strong style='color: #FFFFFF;'>NexusGear</strong> • Curated Gaming Peripherals &amp; Ergonomic Desk Setups</p>");
        sb.AppendLine("</div></div></body></html>");

        return sb.ToString();
    }
}

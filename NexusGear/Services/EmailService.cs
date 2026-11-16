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
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #0B1120; color: #E2E8F0; margin: 0; padding: 24px; }");
        sb.AppendLine(".card { max-width: 600px; margin: 0 auto; background: #131D31; border: 1px solid #1E293B; border-radius: 12px; padding: 24px; }");
        sb.AppendLine(".header { text-align: center; border-bottom: 1px solid #1E293B; padding-bottom: 20px; margin-bottom: 20px; }");
        sb.AppendLine(".title { font-size: 24px; font-weight: bold; color: #FFFFFF; margin: 0 0 6px 0; }");
        sb.AppendLine(".order-tag { font-family: monospace; font-size: 16px; color: #00E5A3; background: #0B1120; padding: 4px 10px; border-radius: 6px; display: inline-block; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 16px; }");
        sb.AppendLine("th { text-align: left; color: #94A3B8; font-size: 12px; text-transform: uppercase; padding: 8px 0; border-bottom: 1px solid #1E293B; }");
        sb.AppendLine("td { padding: 12px 0; border-bottom: 1px solid #1E293B; font-size: 14px; }");
        sb.AppendLine(".total-row { font-size: 16px; font-weight: bold; color: #00E5A3; }");
        sb.AppendLine(".footer { text-align: center; color: #94A3B8; font-size: 12px; margin-top: 24px; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='card'>");
        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<h1 class='title'>⚡ NexusGear — Order Confirmed</h1>");
        sb.AppendLine($"<p style='color: #94A3B8; margin: 0 0 12px 0;'>Thank you for your purchase, {order.User?.FullName}!</p>");
        sb.AppendLine($"<div class='order-tag'>{order.OrderNumber}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div>");
        sb.AppendLine("<p style='margin: 0 0 4px 0; color: #94A3B8; font-size: 12px;'>SHIPPING DESTINATION:</p>");
        sb.AppendLine($"<p style='margin: 0 0 16px 0; font-size: 14px; color: #FFFFFF;'>{order.Address?.Street}, {order.Address?.City}, jud. {order.Address?.County} ({order.Address?.PostalCode})<br>Courier: {order.ShippingMethod}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Product</th><th style='text-align:center;'>Qty</th><th style='text-align:right;'>Price</th></tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var item in order.OrderItems)
        {
            sb.AppendLine($"<tr><td><strong>{item.Product?.Name}</strong></td><td style='text-align:center;'>{item.Quantity}</td><td style='text-align:right;'>{(item.Quantity * item.UnitPrice):N2} RON</td></tr>");
        }
        sb.AppendLine("</tbody>");
        sb.AppendLine("<tfoot>");
        sb.AppendLine($"<tr><td colspan='2' style='text-align:right; color:#94A3B8;'>Delivery:</td><td style='text-align:right;'>{(order.ShippingCost == 0 ? "FREE" : $"{order.ShippingCost:N2} RON")}</td></tr>");
        if (order.DiscountAmount > 0)
        {
            sb.AppendLine($"<tr><td colspan='2' style='text-align:right; color:#00E5A3;'>Discount:</td><td style='text-align:right; color:#00E5A3;'>-{order.DiscountAmount:N2} RON</td></tr>");
        }
        sb.AppendLine($"<tr class='total-row'><td colspan='2' style='text-align:right;'>Total Paid:</td><td style='text-align:right;'>{order.TotalAmount:N2} RON</td></tr>");
        sb.AppendLine("</tfoot></table>");

        sb.AppendLine("<div class='footer'>");
        sb.AppendLine("<p>This email acts as your official electronic purchase receipt.<br>NexusGear • Curated Gaming Peripherals & Ergonomic Desk Setups</p>");
        sb.AppendLine("</div></div></body></html>");

        return sb.ToString();
    }
}

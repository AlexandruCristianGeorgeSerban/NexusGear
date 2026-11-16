using Microsoft.AspNetCore.Mvc;
using NexusGear.Services.Interfaces;

namespace NexusGear.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IWebhookService webhookService, ILogger<WebhookController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    [HttpPost("stripe")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> StripeWebhook()
    {
        string json;
        using (var reader = new StreamReader(HttpContext.Request.Body))
        {
            json = await reader.ReadToEndAsync();
        }

        string? stripeSignature = Request.Headers["Stripe-Signature"];

        var result = await _webhookService.ProcessWebhookAsync(json, stripeSignature);

        if (!result.Success)
        {
            _logger.LogWarning("Stripe webhook verification or execution failed: {Message}", result.Message);
            return BadRequest(new { error = result.Message });
        }

        _logger.LogInformation("Stripe webhook processed successfully for Order: {OrderNumber}", result.OrderNumber);
        return Ok(new
        {
            received = true,
            orderNumber = result.OrderNumber,
            idempotent = result.IsIdempotentDuplicate
        });
    }
}

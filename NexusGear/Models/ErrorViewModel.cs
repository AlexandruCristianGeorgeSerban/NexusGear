namespace NexusGear.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public int StatusCode { get; set; } = 500;

    public string Title => StatusCode switch
    {
        404 => "Page Not Found",
        403 => "Access Forbidden",
        401 => "Authentication Required",
        _ => "An Unexpected Error Occurred"
    };

    public string Description => StatusCode switch
    {
        404 => "The hardware component, peripheral, or resource you are looking for does not exist or may have been relocated.",
        403 => "You do not possess the required security clearances to inspect this internal resource.",
        401 => "Please sign in with your customer or administrator credentials to access this secure zone.",
        _ => "Our engineering telemetry recorded an unexpected condition. Please try again or return to the main catalog."
    };
}

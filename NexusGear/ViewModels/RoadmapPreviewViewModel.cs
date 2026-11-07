namespace NexusGear.ViewModels;

public class RoadmapPreviewViewModel
{
    public string ModuleTitle { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string StageBadge { get; set; } = "Stage 3 (Weeks 8–12)";
    public string IconClass { get; set; } = "bi-gear-wide-connected";
    public List<string> RequirementTags { get; set; } = new();
    public List<string> PlannedFeatures { get; set; } = new();
    public string PrimaryActionUrl { get; set; } = "/Catalog";
    public string PrimaryActionText { get; set; } = "Browse Catalog";
    public string? SecondaryActionUrl { get; set; }
    public string? SecondaryActionText { get; set; }
}


using System.ComponentModel.DataAnnotations;

namespace NexusGear.Models;

public class Review
{
    [Key]
    public int ReviewId { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [StringLength(1000)]
    public string Comment { get; set; } = string.Empty;

    public bool IsHidden { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int UserId { get; set; }

    [Required]
    public int ProductId { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Product? Product { get; set; }
}


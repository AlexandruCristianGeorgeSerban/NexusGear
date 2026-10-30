using System.ComponentModel.DataAnnotations;

namespace NexusGear.Models;

public class CartItem
{
    [Key]
    public int CartItemId { get; set; }

    [Required]
    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int UserId { get; set; }

    [Required]
    public int ProductId { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Product? Product { get; set; }
}


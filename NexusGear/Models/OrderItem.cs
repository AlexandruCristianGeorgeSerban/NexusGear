using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusGear.Models;

public class OrderItem
{
    [Key]
    public int OrderItemId { get; set; }

    [Required]
    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required]
    public int ProductId { get; set; }

    // Navigation properties
    public Order? Order { get; set; }
    public Product? Product { get; set; }
}


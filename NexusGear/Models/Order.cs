using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusGear.Models;

public class Order
{
    [Key]
    public int OrderId { get; set; }

    [Required]
    [StringLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Unpaid"; // "Unpaid", "New", "Paid", "Shipped", "Delivered", "Cancelled"

    [Required]
    [StringLength(50)]
    public string ShippingMethod { get; set; } = "Standard Courier";

    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingCost { get; set; } = 15.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(255)]
    public string? StripeSessionId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int AddressId { get; set; }

    public int? DiscountCodeId { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Address? Address { get; set; }
    public DiscountCode? DiscountCode { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}


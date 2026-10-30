using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusGear.Models;

public class DiscountCode
{
    [Key]
    public int DiscountCodeId { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string DiscountType { get; set; } = "Percentage"; // "Percentage" or "FixedAmount"

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountValue { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    [Required]
    public int MaxUses { get; set; } = 100;

    public int UsedCount { get; set; } = 0;

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}


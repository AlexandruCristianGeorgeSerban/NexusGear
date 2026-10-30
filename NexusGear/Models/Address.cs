using System.ComponentModel.DataAnnotations;

namespace NexusGear.Models;

public class Address
{
    [Key]
    public int AddressId { get; set; }

    [Required]
    [StringLength(150)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string County { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;

    [Required]
    public int UserId { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}


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

    // Navigation properties
    public ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

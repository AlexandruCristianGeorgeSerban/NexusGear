using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusGear.Models;

public class UserAddress
{
    [Key]
    public int UserAddressId { get; set; }

    public bool IsDefault { get; set; } = false;

    [Required]
    public int UserId { get; set; }

    [Required]
    public int AddressId { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Address? Address { get; set; }
}


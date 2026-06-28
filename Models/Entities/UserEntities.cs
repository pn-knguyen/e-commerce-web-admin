using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.Models.Entities;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Gender Gender { get; set; } = Gender.Unknown;
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsActive { get; set; } = true;
    public string? AvatarImage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<VoucherUser> VoucherUsers { get; set; } = new List<VoucherUser>();
    public ICollection<VoucherUsage> VoucherUsages { get; set; } = new List<VoucherUsage>();
    public ICollection<CustomerConversation> CustomerConversations { get; set; } = new List<CustomerConversation>();
    public ICollection<CustomerMessage> CustomerMessages { get; set; } = new List<CustomerMessage>();
}

public class UserAddress
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ProvinceCode { get; set; } = string.Empty;
    public string ProvinceName { get; set; } = string.Empty;
    public string? DistrictCode { get; set; }
    public string? DistrictName { get; set; }
    public string WardCode { get; set; } = string.Empty;
    public string WardName { get; set; } = string.Empty;
    public string DetailAddress { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? PlaceId { get; set; }
    public string? FormattedAddress { get; set; }
    public AddressType Type { get; set; } = AddressType.Shipping;
    public bool IsDefault { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class CartItem
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User? User { get; set; }
    public ProductVariant? ProductVariant { get; set; }
}

public class Wishlist
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long ProductVariantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public ProductVariant? ProductVariant { get; set; }
}

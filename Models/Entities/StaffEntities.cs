using Microsoft.AspNetCore.Identity;

namespace e_commerce_web_admin.Models.Entities;

public class Staff : IdentityUser<long>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? AvatarImage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<GoodsReceipt> CreatedGoodsReceipts { get; set; } = new List<GoodsReceipt>();
    public ICollection<GoodsReceipt> ApprovedGoodsReceipts { get; set; } = new List<GoodsReceipt>();
    public ICollection<Shipment> RequestedShipments { get; set; } = new List<Shipment>();
}

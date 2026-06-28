namespace e_commerce_web_admin.Models.Enums;

public enum Gender
{
    Unknown = 0,
    Male = 1,
    Female = 2,
    Other = 3
}

public enum UserRole
{
    Customer = 0,
    Staff = 1,
    Manager = 2,
    Admin = 3
}

public enum AddressType
{
    Shipping = 0,
    Billing = 1,
    Other = 2
}

public enum DiscountType
{
    FixedAmount = 0,
    Percentage = 1
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipping = 3,
    Completed = 4,
    Cancelled = 5,
    Returned = 6
}

public enum PaymentStatus
{
    Unpaid = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}

public enum ShippingProvider
{
    GiaoHangNhanh = 0
}

public enum ShipmentStatus
{
    Draft = 0,
    Quoted = 1,
    Booked = 2,
    PickingUp = 3,
    InTransit = 4,
    Delivered = 5,
    Cancelled = 6,
    Failed = 7,
    ReadyToPick = 8,
    Picking = 9,
    MoneyCollectPicking = 10,
    Picked = 11,
    Storing = 12,
    Transporting = 13,
    Sorting = 14,
    Delivering = 15,
    MoneyCollectDelivering = 16,
    DeliveryFail = 17,
    WaitingToReturn = 18,
    Return = 19,
    ReturnTransporting = 20,
    ReturnSorting = 21,
    Returning = 22,
    ReturnFail = 23,
    Returned = 24,
    Exception = 25,
    Damage = 26,
    Lost = 27,
    ProviderUnknown = 28,
    Booking = 29
}

public enum CampaignType
{
    Banner = 0,
    Category = 1,
    FlashSale = 2,
    Seasonal = 3
}

public enum TargetType
{
    Product = 0,
    ProductVariant = 1,
    Category = 2,
    Brand = 3,
    User = 4
}

public enum PromotionActionType
{
    DiscountOrder = 0,
    DiscountProduct = 1,
    BuyXGetY = 2,
    GiftProduct = 3
}

public enum GoodsReceiptStatus
{
    Draft = 0,
    Pending = 1,
    Approved = 2,
    Cancelled = 3
}

public enum CustomerConversationStatus
{
    Open = 0,
    AwaitingCustomer = 1,
    Closed = 2
}

public enum CustomerMessageSender
{
    Customer = 0,
    Staff = 1,
    Ai = 2
}

public enum CustomerConversationChannel
{
    Support = 0,
    Ai = 1
}

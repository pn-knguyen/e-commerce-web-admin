namespace e_commerce_web_admin.Models.Validation;

public static class PromotionValidationRules
{
    public const int NameMaxLength = 255;
    public const int DescriptionMaxLength = 1000;
    public const double PositiveAmountMin = 0.01;
    public const int PositiveIntegerMin = 1;
    public const int NonNegativeIntegerMin = 0;
    public const int PriorityMin = 0;
    public const int PriorityMax = 9999;
}

public static class PromotionValidationMessages
{
    public const string NameRequired = "Tên khuyến mãi là bắt buộc.";
    public const string NameMaxLength = "Tên khuyến mãi tối đa 255 ký tự.";
    public const string DescriptionMaxLength = "Mô tả tối đa 1000 ký tự.";
    public const string MinOrderRequired = "Đơn tối thiểu là bắt buộc.";
    public const string MinOrderNonNegative = "Đơn tối thiểu không được âm.";
    public const string MaxDiscountPositive = "Mức giảm tối đa phải lớn hơn 0.";
    public const string UsageLimitPositive = "Giới hạn sử dụng phải lớn hơn 0.";
    public const string UsageLimitLessThanUsed = "Giới hạn sử dụng không được nhỏ hơn số lượt đã dùng ({0}).";
    public const string TargetTypeInvalid = "Loại phạm vi áp dụng không hợp lệ.";
    public const string TargetRequired = "Vui lòng chọn ít nhất một phạm vi áp dụng.";
    public const string TargetInvalid = "Phạm vi áp dụng đã chọn không hợp lệ.";
    public const string StartDateRequired = "Ngày bắt đầu là bắt buộc.";
    public const string EndDateRequired = "Ngày kết thúc là bắt buộc.";
    public const string EndDateAfterStart = "Ngày kết thúc phải sau ngày bắt đầu.";
    public const string PriorityRequired = "Độ ưu tiên là bắt buộc.";
    public const string PriorityRange = "Độ ưu tiên phải từ 0 đến 9999.";
    public const string ActionTypeRequired = "Loại khuyến mãi là bắt buộc.";
    public const string ActionTypeInvalid = "Loại khuyến mãi không hợp lệ.";
    public const string DiscountValueRequired = "Giá trị giảm là bắt buộc.";
    public const string DiscountValuePositive = "Giá trị giảm phải lớn hơn 0.";
    public const string DiscountValueNonNegative = "Giá trị giảm không được âm.";
    public const string BuyQuantityRequired = "Số lượng mua là bắt buộc.";
    public const string BuyQuantityPositive = "Số lượng mua phải lớn hơn 0.";
    public const string GetQuantityRequired = "Số lượng nhận là bắt buộc.";
    public const string GetQuantityNonNegative = "Số lượng nhận không được âm.";
    public const string GiftQuantityPositive = "Số lượng quà tặng phải lớn hơn 0.";
    public const string GiftVariantRequired = "Sản phẩm quà tặng là bắt buộc.";
    public const string GiftVariantInvalid = "Sản phẩm quà tặng không hợp lệ.";
    public const string BuyXGetYRequiresBenefit = "Mua X nhận Y cần có giá trị giảm hoặc số lượng nhận lớn hơn 0.";
    public const string MaxDiscountLessThanDiscount = "Mức giảm tối đa không được nhỏ hơn giá trị giảm.";
}

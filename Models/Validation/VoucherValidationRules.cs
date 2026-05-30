namespace e_commerce_web_admin.Models.Validation;

public static class VoucherValidationRules
{
    public const int CodeMaxLength = 80;
    public const int CodeInputMaxLength = 120;
    public const string CodePattern = @"^[A-Za-z0-9][A-Za-z0-9_-]*$";

    public const double PositiveAmountMin = 0.01;
    public const int PositiveIntegerMin = 1;
    public const int PriorityMin = 0;
    public const int PriorityMax = 9999;
    public const int PercentageDiscountMax = 100;
}

public static class VoucherValidationMessages
{
    public const string CodeRequired = "Mã voucher là bắt buộc.";
    public const string CodeMaxLength = "Mã voucher tối đa 80 ký tự.";
    public const string CodePattern =
        "Mã voucher phải bắt đầu bằng chữ cái hoặc số; các ký tự tiếp theo chỉ gồm chữ cái, số, dấu gạch ngang hoặc gạch dưới.";
    public const string DescriptionMaxLength = "Mô tả tối đa 1000 ký tự.";
    public const string DiscountTypeRequired = "Loại giảm giá là bắt buộc.";
    public const string DiscountTypeInvalid = "Loại giảm giá không hợp lệ.";
    public const string DiscountValueRequired = "Giá trị giảm là bắt buộc.";
    public const string DiscountValuePositive = "Giá trị giảm phải lớn hơn 0.";
    public const string MinOrderRequired = "Đơn tối thiểu là bắt buộc.";
    public const string MinOrderNonNegative = "Giá trị đơn tối thiểu không được âm.";
    public const string MaxDiscountPositive = "Mức giảm tối đa phải lớn hơn 0.";
    public const string MaxUsesPositive = "Tổng lượt dùng phải lớn hơn 0.";
    public const string MaxUsesPerUserPositive = "Lượt dùng mỗi khách phải lớn hơn 0.";
    public const string StartDateRequired = "Ngày bắt đầu là bắt buộc.";
    public const string EndDateRequired = "Ngày kết thúc là bắt buộc.";
    public const string EndDateAfterStart = "Ngày kết thúc phải sau ngày bắt đầu.";
    public const string PriorityRequired = "Độ ưu tiên là bắt buộc.";
    public const string PriorityRange = "Độ ưu tiên phải từ 0 đến 9999.";
    public const string DuplicateCode = "Mã voucher đã tồn tại, hãy dùng mã khác.";
    public const string PercentageDiscountMax = "Giảm theo phần trăm không được vượt quá 100%.";
    public const string FixedMaxDiscount =
        "Mức giảm tối đa không được nhỏ hơn giá trị giảm cố định.";
    public const string MaxUsesLessThanUsed =
        "Tổng lượt dùng không được nhỏ hơn số lượt đã dùng ({0}).";
    public const string MaxUsesPerUserExceedsMaxUses =
        "Lượt dùng mỗi khách không được lớn hơn tổng lượt dùng.";
}

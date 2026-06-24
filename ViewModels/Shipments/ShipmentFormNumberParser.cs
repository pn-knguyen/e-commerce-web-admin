using System.Globalization;

namespace e_commerce_web_admin.ViewModels.Shipments;

public static class ShipmentFormNumberParser
{
    public static bool TryParseDecimal(string value, out decimal result)
    {
        var normalized = NormalizeDecimalText(value);
        if (decimal.TryParse(
                normalized,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out result))
        {
            return true;
        }

        foreach (var culture in new[]
                 {
                     CultureInfo.InvariantCulture,
                     CultureInfo.GetCultureInfo("vi-VN"),
                     CultureInfo.GetCultureInfo("en-US"),
                 })
        {
            if (decimal.TryParse(value, NumberStyles.Number, culture, out result))
            {
                return true;
            }
        }

        result = default;
        return false;
    }

    public static string Format(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string NormalizeDecimalText(string value)
    {
        var text = value.Trim()
            .Replace(" ", string.Empty)
            .Replace("'", string.Empty);

        var lastDot = text.LastIndexOf('.');
        var lastComma = text.LastIndexOf(',');
        if (lastDot >= 0 && lastComma >= 0)
        {
            var decimalSeparator = lastDot > lastComma ? '.' : ',';
            var groupSeparator = decimalSeparator == '.' ? "," : ".";
            text = text.Replace(groupSeparator, string.Empty);
            return decimalSeparator == ',' ? text.Replace(',', '.') : text;
        }

        if (lastComma >= 0 && lastDot < 0 && text.Count(character => character == ',') == 1)
        {
            return text.Replace(',', '.');
        }

        return text;
    }
}

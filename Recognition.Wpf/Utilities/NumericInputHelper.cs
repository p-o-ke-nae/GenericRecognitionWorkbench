using System.Globalization;

namespace Recognition.Wpf;

internal static class NumericInputHelper
{
    public static bool IsValidProposedText(string text, bool allowsDecimal, double? minimum = null)
    {
        var normalized = Normalize(text);
        if (string.IsNullOrEmpty(normalized))
        {
            return true;
        }

        var allowsNegative = minimum is null || minimum < 0;
        if (normalized is "-" or "+")
        {
            return allowsNegative;
        }

        if (allowsDecimal)
        {
            var separatorCount = normalized.Count(static c => c == '.');
            if (separatorCount > 1)
            {
                return false;
            }

            if (!normalized.All(static c => char.IsDigit(c) || c is '.' or '-' or '+'))
            {
                return false;
            }

            if ((normalized.Contains('-') || normalized.Contains('+')) && normalized[0] is not '-' and not '+')
            {
                return false;
            }

            return allowsNegative || (!normalized.StartsWith('-') && !normalized.StartsWith('+'));
        }

        if (!normalized.All(static c => char.IsDigit(c) || c is '-' or '+'))
        {
            return false;
        }

        if ((normalized.Contains('-') || normalized.Contains('+')) && normalized[0] is not '-' and not '+')
        {
            return false;
        }

        return allowsNegative || (!normalized.StartsWith('-') && !normalized.StartsWith('+'));
    }

    public static string Normalize(string text)
    {
        return (text ?? string.Empty).Replace(',', '.');
    }

    public static string StepValue(
        string currentText,
        string defaultText,
        bool allowsDecimal,
        double step,
        int direction,
        double? minimum = null,
        double? maximum = null)
    {
        var fallback = ParseOrDefault(defaultText, allowsDecimal ? 0d : 0d);
        var current = ParseOrDefault(currentText, fallback);
        var next = current + (step * direction);
        if (minimum is not null)
        {
            next = Math.Max(next, minimum.Value);
        }

        if (maximum is not null)
        {
            next = Math.Min(next, maximum.Value);
        }

        return Format(next, allowsDecimal, step);
    }

    public static int ParseInt32OrDefault(string text, int defaultValue)
    {
        return int.TryParse(Normalize(text), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }

    public static double ParseDoubleOrDefault(string text, double defaultValue)
    {
        return double.TryParse(Normalize(text), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static double ParseOrDefault(string text, double defaultValue)
    {
        return double.TryParse(Normalize(text), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static string Format(double value, bool allowsDecimal, double step)
    {
        if (!allowsDecimal)
        {
            return Math.Round(value).ToString(CultureInfo.InvariantCulture);
        }

        var decimals = CountDecimals(step);
        return Math.Round(value, decimals).ToString($"F{decimals}", CultureInfo.InvariantCulture);
    }

    private static int CountDecimals(double step)
    {
        var text = step.ToString("0.################", CultureInfo.InvariantCulture);
        var separatorIndex = text.IndexOf('.');
        return separatorIndex < 0 ? 0 : text.Length - separatorIndex - 1;
    }
}

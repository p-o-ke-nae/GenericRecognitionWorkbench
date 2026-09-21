using System.Globalization;

namespace Recognition.Core;

public static class ComponentParameterReader
{
    public static string GetString(this IReadOnlyDictionary<string, string> parameters, string key, string defaultValue = "")
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;
    }

    public static int GetInt32(this IReadOnlyDictionary<string, string> parameters, string key, int defaultValue = 0)
    {
        return parameters.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }

    public static double GetDouble(this IReadOnlyDictionary<string, string> parameters, string key, double defaultValue = 0)
    {
        return parameters.TryGetValue(key, out var value)
            && double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : defaultValue;
    }

    public static bool GetBoolean(this IReadOnlyDictionary<string, string> parameters, string key, bool defaultValue = false)
    {
        return parameters.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : defaultValue;
    }

    public static RoiArea GetRoi(this IReadOnlyDictionary<string, string> parameters, string prefix)
    {
        var x = parameters.GetInt32($"{prefix}X");
        var y = parameters.GetInt32($"{prefix}Y");
        var width = parameters.GetInt32($"{prefix}Width");
        var height = parameters.GetInt32($"{prefix}Height");

        return width > 0 && height > 0
            ? new RoiArea(x, y, width, height)
            : RoiArea.Empty;
    }
}

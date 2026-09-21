using System.Management;
using Recognition.Core;

namespace Recognition.Infrastructure;

internal static class CameraDeviceCatalog
{
    public static IReadOnlyList<ParameterOption> GetCameraOptions()
    {
        var names = GetCameraNames();
        var optionCount = Math.Max(10, names.Count);
        var options = new List<ParameterOption>(optionCount);
        for (var index = 0; index < optionCount; index++)
        {
            var label = index < names.Count && !string.IsNullOrWhiteSpace(names[index])
                ? $"{index}: {names[index]}"
                : $"Camera {index}";
            options.Add(new ParameterOption(index.ToString(), label));
        }

        return options;
    }

    private static IReadOnlyList<string> GetCameraNames()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_PnPEntity WHERE PNPClass = 'Camera' OR Service = 'usbvideo'");
            return searcher
                .Get()
                .Cast<ManagementBaseObject>()
                .Select(static device => device["Name"]?.ToString())
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .Select(static name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }
}

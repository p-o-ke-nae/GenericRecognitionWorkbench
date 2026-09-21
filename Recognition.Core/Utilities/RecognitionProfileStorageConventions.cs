namespace Recognition.Core;

public static class RecognitionProfileStorageConventions
{
    public const string ProfileFileName = "profile.json";
    public const string ImagesDirectoryName = "images";

    public static string GetProfileFilePath(string profileDirectory)
    {
        return Path.Combine(profileDirectory, ProfileFileName);
    }

    public static bool HasProfileFileName(string filePath)
    {
        return string.Equals(Path.GetFileName(filePath), ProfileFileName, StringComparison.OrdinalIgnoreCase);
    }
}

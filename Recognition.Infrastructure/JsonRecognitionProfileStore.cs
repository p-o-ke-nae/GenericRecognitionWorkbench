using System.Text.Json;
using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class JsonRecognitionProfileStore : IRecognitionProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public async Task SaveAsync(RecognitionProfile profile, string filePath, CancellationToken cancellationToken = default)
    {
        ValidateProfileFilePath(filePath);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, profile, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RecognitionProfile> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ValidateProfileFilePath(filePath);
        await using var stream = File.OpenRead(filePath);
        var profile = await JsonSerializer.DeserializeAsync<RecognitionProfile>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
        return profile ?? throw new InvalidOperationException("Profile file could not be deserialized.");
    }

    private static void ValidateProfileFilePath(string filePath)
    {
        if (!RecognitionProfileStorageConventions.HasProfileFileName(filePath))
        {
            throw new InvalidOperationException($"Profile files must be stored as '{RecognitionProfileStorageConventions.ProfileFileName}' inside a dedicated profile folder.");
        }
    }
}

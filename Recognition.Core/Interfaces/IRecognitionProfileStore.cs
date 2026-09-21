namespace Recognition.Core;

public interface IRecognitionProfileStore
{
    Task SaveAsync(RecognitionProfile profile, string filePath, CancellationToken cancellationToken = default);

    Task<RecognitionProfile> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}

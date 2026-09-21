using Recognition.Core;

namespace Recognition.Infrastructure;

public sealed class CameraFrameSourceFactory : IFrameSourceFactory
{
    public ComponentDescriptor Descriptor => new(
        "builtin.camera",
        "Camera Capture",
        "Captures frames from a camera using OpenCV VideoCapture.",
        [
            new ParameterDefinition("CameraIndex", "Camera", ParameterValueKind.Choice, "0", Options: CameraDeviceCatalog.GetCameraOptions()),
            new ParameterDefinition("Width", "Width", ParameterValueKind.Integer, "1280", Minimum: 1, Step: 10),
            new ParameterDefinition("Height", "Height", ParameterValueKind.Integer, "720", Minimum: 1, Step: 10),
            new ParameterDefinition("Fps", "Target Camera FPS", ParameterValueKind.Integer, "60", Minimum: 1, Step: 1)
        ]);

    public IFrameSource Create(IReadOnlyDictionary<string, string> parameters)
    {
        return new CameraFrameSource(
            parameters.GetInt32("CameraIndex"),
            parameters.GetInt32("Width", 1280),
            parameters.GetInt32("Height", 720),
            parameters.GetInt32("Fps", 60));
    }
}

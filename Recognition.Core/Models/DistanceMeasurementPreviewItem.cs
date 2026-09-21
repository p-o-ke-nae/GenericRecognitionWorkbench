namespace Recognition.Core;

public sealed class DistanceMeasurementPreviewItem
{
    public DistanceMeasurementPreviewItem(
        string referenceName,
        RoiArea referenceRegion,
        string targetName,
        RoiArea targetRegion,
        int deltaX,
        int deltaY)
    {
        ReferenceName = referenceName;
        ReferenceRegion = referenceRegion;
        TargetName = targetName;
        TargetRegion = targetRegion;
        DeltaX = deltaX;
        DeltaY = deltaY;
    }

    public string ReferenceName { get; }

    public RoiArea ReferenceRegion { get; }

    public string TargetName { get; }

    public RoiArea TargetRegion { get; }

    public int DeltaX { get; }

    public int DeltaY { get; }
}

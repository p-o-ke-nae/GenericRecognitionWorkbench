namespace Recognition.Core;

public readonly record struct RoiArea(int X, int Y, int Width, int Height)
{
    public static RoiArea Empty => new(0, 0, 0, 0);

    public bool IsEmpty => Width <= 0 || Height <= 0;
}

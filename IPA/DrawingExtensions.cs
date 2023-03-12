using System.Drawing;

namespace IPA;

public static class DrawingExtensions
{
    /// <summary>
    /// For binary images
    /// </summary>
    public static bool IsForeground(this Color color) => color is { R: >= 245, G: >= 245, B: >= 245 };
}
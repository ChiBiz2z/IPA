using System.Drawing;

namespace IPA;

public static class ColorExtensions
{
    /// <summary>
    /// For binary images
    /// </summary>
    public static bool IsForeground(this Color color) => color is { R: 255, G: 255, B: 255 };
}
using System.Globalization;

namespace MP.DxpEnvironmentIndicator.Services;

public static class ContrastColor
{
    // Near-black; used as the badge text on light backgrounds.
    public const string Dark = "#1a1a1a";
    // White; used as the badge text on dark backgrounds.
    public const string Light = "#ffffff";

    // The WCAG relative-luminance crossover where black and white text have equal contrast against
    // the background (solving (1.05)/(L+0.05) = (L+0.05)/0.05 gives L = 0.179). Above it, dark text
    // is the more legible choice; below it, white.
    private const double LuminanceThreshold = 0.179;

    // Returns the more accessible badge text colour for the given background hex (#rgb or #rrggbb).
    public static string Text(string hexBackground)
    {
        if (!TryParse(hexBackground, out var r, out var g, out var b))
            return Light; // sensible default for a missing/invalid colour

        var luminance = 0.2126 * Linearise(r) + 0.7152 * Linearise(g) + 0.0722 * Linearise(b);
        return luminance > LuminanceThreshold ? Dark : Light;
    }

    // sRGB channel (0-255) to its linear-light value, per the WCAG relative-luminance definition.
    private static double Linearise(int channel)
    {
        var c = channel / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    private static bool TryParse(string hex, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        hex = hex.Trim().TrimStart('#');
        if (hex.Length == 3)
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        if (hex.Length != 6) return false;

        return int.TryParse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
             & int.TryParse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
             & int.TryParse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
    }
}

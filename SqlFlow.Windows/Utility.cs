using System.Linq;
using System.Windows.Media;

namespace SqlFlow;

public static class Utility
{
    public static Color GetColorFromString(string? color)
    {
        if (color is null || color.Split('|').Length != 3)
            return Colors.Black;
        var array = color.Split('|').Select(int.Parse).Select(v => (byte)v).ToArray();
        return Color.FromRgb(array[0], array[1], array[2]);
    }
}

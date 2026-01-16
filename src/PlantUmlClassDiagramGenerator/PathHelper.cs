using System.IO;

namespace PlantUmlClassDiagramGenerator;

public static class PathHelper
{
    public static string CombinePath(string first, string second)
    {
        first = first.TrimEnd(Path.DirectorySeparatorChar);
        second = second.TrimStart(Path.DirectorySeparatorChar);
        return string.IsNullOrEmpty(first) ? second : string.IsNullOrEmpty(second) ? first
            : first + Path.DirectorySeparatorChar + second;
    }
}

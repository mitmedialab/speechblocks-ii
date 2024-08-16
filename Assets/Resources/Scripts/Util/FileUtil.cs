using System.Collections.Generic;
using System.IO;

public class FileUtil
{
    public static IEnumerator<string> EnumerateFilesRecursively(string path)
    {
        if (!Directory.Exists(path) || path.EndsWith(".DS_Store")) return _Empty();
        return _EnumerateFilesRecursively(path);
    }

    public static IEnumerator<string> _Empty() { yield break; }

    private static IEnumerator<string> _EnumerateFilesRecursively(string path)
    {
        foreach (string filepath in Directory.GetFiles(path))
        {
            if (!filepath.EndsWith(".DS_Store")) yield return filepath;
        }
        foreach (string filepath in Directory.GetDirectories(path))
        {
            IEnumerator<string> subEnum = _EnumerateFilesRecursively(filepath);
            while (subEnum.MoveNext())
            {
                yield return subEnum.Current;
            }
        }
    }
}

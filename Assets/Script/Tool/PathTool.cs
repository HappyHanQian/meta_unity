using System.IO;
using UnityEngine;

public class PathTool
{
    public static string AssetPath2FullPath(string assetPath)
    {
        var path = Path.Combine(Application.dataPath, assetPath.Replace("Assets", "."));
        return path.Replace("/", "\\");
    }

    public static string FullPath2AssetPath(string fullPath)
    {
        int index = fullPath.IndexOf("Assets");
        if (index >= 0)
        {
            string assetpath = fullPath.Substring(index);
            return assetpath.Replace("\\", "/");
        }

        return "";
    }
}
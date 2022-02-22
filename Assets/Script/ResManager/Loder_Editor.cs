#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class Loder_Editor : ResLoader
{
    private Dictionary<string, string> allAssets = new Dictionary<string, string>();

    public void Init(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("res init error,path is null or empty!!!");
            return;
        }

        DirectoryInfo dir = new DirectoryInfo(Application.dataPath);
        var files = dir.GetFiles("*.*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            if (!IsValidAsset(file))
            {
                continue;
            }

            if (allAssets.ContainsKey(file.Name))
            {
                Debug.LogError(
                    $"have same name asset:{file.Name},path1:{allAssets[file.Name]},path2:{PathTool.FullPath2AssetPath(file.FullName)}");
            }
            else
            {
                allAssets.Add(file.Name, PathTool.FullPath2AssetPath(file.FullName));
            }
        }
    }

    private bool IsValidAsset(FileInfo f)
    {
        if (f.FullName.Contains("Editor"))
        {
            return false;
        }

        if (f.FullName.Contains("StreamingAssets"))
        {
            return false;
        }

        if (f.FullName.Contains("Plugins"))
        {
            return false;
        }

        switch (f.Extension)
        {
            case ".cs":
            case ".meta":
            case ".dll":
                return false;
            default:
                return true;
        }
    }

    public T LoadAsset<T>(string assetName) where T : Object
    {
        if (allAssets.ContainsKey(assetName))
        {
            return AssetDatabase.LoadAssetAtPath<T>(allAssets[assetName]);
        }
        else
        {
            return null;
        }
    }

    public IEnumerator LoadAssetAsync<T>(string assetname, Action<T> callBack) where T : Object
    {
        yield return new WaitForSecondsRealtime(1f);
        var result = LoadAsset<T>(assetname);
        callBack?.Invoke(result);
    }
}
#endif
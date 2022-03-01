using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class AssetBundleInfos
{
    /// <summary>
    /// <name,<variant,bundleInfo>>
    /// </summary>
    public Dictionary<string, Dictionary<string, AssetBundleInfo>> bundlesDic;

    public Dictionary<string, Asset_Bundle> allAssets;
    string curRootAsset = string.Empty;
    float curProgress = 0f;
    public int totalBundleNum;

    public AssetBundleInfos()
    {
        this.allAssets = new Dictionary<string, Asset_Bundle>();
        this.bundlesDic = new Dictionary<string, Dictionary<string, AssetBundleInfo>>();
    }

    public bool IsExistName(string renameValue)
    {
        return bundlesDic.ContainsKey(renameValue);
    }

    public void Clear()
    {
        bundlesDic.Clear();
    }

    public Asset_Bundle GetBundleAsset(string assetpath)
    {
        if (allAssets.ContainsKey(assetpath))
        {
            return allAssets[assetpath];
        }

        return null;
    }

    /// <summary>
    /// 分析依赖创建ab结构
    /// </summary>
    public void Creat()
    {
        DeleteBundleInfo();
        CreatAllAsset(Application.dataPath);
        CreatAssetBundls();
        CreatBundleInfo();
        AssetDatabase.Refresh();
    }


    private void CreatAssetBundls()
    {
        totalBundleNum = 0;
        foreach (string assetsKey in allAssets.Keys)
        {
            var value = allAssets[assetsKey];
            // if (string.IsNullOrEmpty(value.bundled))
            // {
            //     continue;
            // }
            AddToBundleDic(value);
        }
    }

    private void AddToBundleDic(Asset_Bundle value)
    {
        if (bundlesDic.ContainsKey(value.bundled))
        {
            var variant = bundlesDic[value.bundled];
            if (variant.ContainsKey(value.variant))
            {
                variant[value.variant].AddAsset(value);
            }
            else
            {
                AssetBundleInfo ab = new AssetBundleInfo(value.bundled, value.variant);
                ab.AddAsset(value);
                variant.Add(ab.variant, ab);
                totalBundleNum++;
            }
        }
        else
        {
            Dictionary<string, AssetBundleInfo> temp = new Dictionary<string, AssetBundleInfo>();
            AssetBundleInfo ab = new AssetBundleInfo(value.bundled, value.variant);
            ab.AddAsset(value);
            temp.Add(ab.variant, ab);
            bundlesDic.Add(ab.name, temp);
            totalBundleNum++;
        }
    }

    private void CreatAllAsset(string path)
    {
        allAssets.Clear();
        DirectoryInfo dir = new DirectoryInfo(path);
        FileInfo[] fs = dir.GetFiles("*.*", SearchOption.AllDirectories);
        int ind = 0;
        for (int i = 0; i < fs.Length; i++)
        {
            var f = fs[i];
            curProgress = (float) ind / (float) fs.Length;
            curRootAsset = "正在分析依赖：" + f.Name;
            EditorUtility.DisplayProgressBar(curRootAsset, curRootAsset, curProgress);
            ind++;
            if (!AssetBundleTool.isValidBundleAsset(f))
            {
                //不需要打进ab包的文件
                continue;
            }

            string assetpath = AssetTool.FullPath2AssetPath(f.FullName);
            if (allAssets.ContainsKey(assetpath))
            {
                continue;
            }

            Asset_Bundle info = new Asset_Bundle(f.FullName, f.Name, f.Extension);
            if (f.Extension == ".unity")
            {
                //场景不用分析依赖
                allAssets.Add(assetpath, info);
            }
            else
            {
                CreateDeps(info);
            }
        }

        EditorUtility.ClearProgressBar();
        AssetBundleTool.ReadCustom();
        int setIndex = 0;
        foreach (KeyValuePair<string, Asset_Bundle> kv in allAssets)
        {
            EditorUtility.DisplayProgressBar("正在设置ABName", kv.Key, (float) setIndex / (float) allAssets.Count);
            setIndex++;
            Asset_Bundle a = kv.Value;
            a.SetAssetBundleNameBuRule();
        }

        EditorUtility.ClearProgressBar();
    }

    private void DeleteBundleInfo()
    {
        string path = Path.Combine(Application.dataPath, "BundleInfo.txt");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void CreatBundleInfo()
    {
        Dictionary<string, Dictionary<string, int>> bundle_ids = new Dictionary<string, Dictionary<string, int>>();
        var keys = bundlesDic.Keys.ToArray();
        int id = 1; //id为0的bundle为manifest bundle
        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var variantKeys = bundlesDic[key].Keys.ToArray();
            for (int j = 0; j < variantKeys.Length; j++)
            {
                var variant_key = variantKeys[j];
                if (bundle_ids.ContainsKey(key))
                {
                    bundle_ids[key].Add(variant_key, id);
                }
                else
                {
                    Dictionary<string, int> variantDic = new Dictionary<string, int>();
                    variantDic.Add(variant_key, id);
                    bundle_ids.Add(key, variantDic);
                }

                id++;
            }
        }
        AssetBundleTool.CreatBundleInfoFile(Application.dataPath, this, bundle_ids);
        FileInfo f = new FileInfo(Path.Combine(Application.dataPath, "BundleInfo.txt"));
        Asset_Bundle info = new Asset_Bundle(f.FullName, f.Name, f.Extension);
        info.SetAssetBundleNameBuRule();
        if (!bundle_ids.ContainsKey(info.bundled))
        {
            Dictionary<string, int> tem = new Dictionary<string, int>();
            tem.Add(info.variant, id);
            bundle_ids.Add(info.bundled, tem);
        }

        if (!allAssets.ContainsKey(info.assetPath))
        {
            allAssets.Add(info.assetPath, info);
            AddToBundleDic(info);
        }
    }

    /// <summary>
    /// 递归分析每个所被依赖到的资源
    /// </summary>
    /// <param name="self"></param>
    /// <param name="parent"></param>
    void CreateDeps(Asset_Bundle self, Asset_Bundle parent = null)
    {
        if (self.HasParent(parent))
            return;
        if (allAssets.ContainsKey(self.assetPath) == false)
        {
            allAssets.Add(self.assetPath, self);
        }

        self.AddParent(parent);
        string[] deps = AssetDatabase.GetDependencies(self.assetPath);
        for (int i = 0; i < deps.Length; i++)
        {
            string assetpath = deps[i];
            if (!assetpath.StartsWith("Assets"))
            {
                continue;
            }

            string fullpath = AssetTool.AssetPath2FullPath(assetpath);
            FileInfo f = new FileInfo(fullpath);
            if (f.Attributes == FileAttributes.Directory)
            {
                //文件夹 不需要打包
                continue;
            }

            if (!AssetBundleTool.isValidBundleAsset(f))
                continue;
            if (assetpath == self.assetPath)
                continue;
            Asset_Bundle info = null;
            if (allAssets.ContainsKey(assetpath))
            {
                info = allAssets[assetpath];
            }
            else
            {
                info = new Asset_Bundle(fullpath, f.Name, f.Extension);
                allAssets.Add(assetpath, info);
            }

            EditorUtility.DisplayProgressBar(curRootAsset, assetpath, curProgress);
            CreateDeps(info, self);
        }
    }

    public List<AssetBundleBuild> GetAssetBundleBuildInfo(out List<string> abNames)
    {
        if (bundlesDic.Count <= 0)
        {
            Creat();
        }

        abNames = new List<string>();
        List<AssetBundleBuild> bundles = new List<AssetBundleBuild>();
        foreach (var bundleNames in bundlesDic)
        {
            foreach (var bundleInfo in bundleNames.Value)
            {
                abNames.Add(bundleNames.Key);
                AssetBundleBuild b = new AssetBundleBuild();
                b.assetBundleName = bundleNames.Key;
                b.assetBundleVariant = bundleInfo.Key;
                b.addressableNames = new string[bundleInfo.Value.assets.Count];
                b.assetNames = new string[bundleInfo.Value.assets.Count];
                for (int i = 0; i < bundleInfo.Value.assets.Count; i++)
                {
                    b.addressableNames[i] = bundleInfo.Value.assets[i].assetName;
                    b.assetNames[i] = bundleInfo.Value.assets[i].assetPath;
                }

                bundles.Add(b);
            }
        }

        return bundles;
    }
}
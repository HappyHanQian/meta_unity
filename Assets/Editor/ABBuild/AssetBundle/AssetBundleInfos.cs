using System.Collections.Generic;
using System.IO;
using ABBuild.Base;
using UnityEditor;
using UnityEngine;

namespace ABBuild
{
    public class AssetBundleInfos
    {
        public List<AssetBundleInfo> assetBundles;
        public Dictionary<string,Asset_Bundle> allAssets;
        string curRootAsset = string.Empty;
        float curProgress = 0f;
        public AssetBundleInfos()
        {
            this.assetBundles = new List<AssetBundleInfo>();
            allAssets = new Dictionary<string, Asset_Bundle>();
        }
        public bool IsExistName(string renameValue)
        {
            for (int i = 0; i < assetBundles.Count; i++)
            {
                if (assetBundles[i].name == renameValue)
                {
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            assetBundles.Clear();
        }
        /// <summary>
        /// 分析依赖创建ab结构
        /// </summary>
        public void Creat(string scr_path)
        {
            CreatAllAsset(scr_path);
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
                curProgress = (float)ind / (float)fs.Length;
                curRootAsset = "正在分析依赖：" + f.Name;
                EditorUtility.DisplayProgressBar(curRootAsset, curRootAsset, curProgress);
                ind++;
                int index = f.FullName.IndexOf("Assets");
                if (index != -1)
                {
                    string assetPath = f.FullName.Substring(index);
                    Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (allAssets.ContainsKey(assetPath) == false
                        && assetPath.StartsWith("Assets")
                        && !(asset is MonoScript)
                        && !(asset is LightingDataAsset)
                        && asset != null
                    )
                    {
                        Asset_Bundle info = new Asset_Bundle(f.FullName,f.Name, f.Extension);
                        //标记一下是文件夹下根资源
                        CreateDeps(info);
                    }
                    EditorUtility.UnloadUnusedAssetsImmediate();
                }
                EditorUtility.UnloadUnusedAssetsImmediate();
                EditorUtility.ClearProgressBar();

                int setIndex = 0;
                foreach (KeyValuePair<string, Asset_Bundle> kv in allAssets)
                {
                    EditorUtility.DisplayProgressBar("正在设置ABName", kv.Key, (float)setIndex / (float)allAssets.Count);
                    setIndex++;
                    Asset_Bundle a = kv.Value;
                    a.SetAssetBundleName(2);
                }
                EditorUtility.ClearProgressBar();
                EditorUtility.UnloadUnusedAssetsImmediate();
                AssetDatabase.SaveAssets();
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

            Object[] deps = EditorUtility.CollectDependencies(new Object[] { self.GetAsset() });
            for (int i = 0; i < deps.Length; i++)
            {
                Object o = deps[i];
                if (o is MonoScript || o is LightingDataAsset)
                    continue;
                string path = AssetDatabase.GetAssetPath(o);
                if (path == self.assetPath)
                    continue;
                if (path.StartsWith("Assets") == false)
                    continue;
                Asset_Bundle info = null;
                if (allAssets.ContainsKey(path))
                {
                    info = allAssets[path];
                }
                else
                {
                    string fullpath = AssetTool.AssetPath2FullPath(path);
                    FileInfo f = new FileInfo(fullpath);
                    info = new Asset_Bundle(fullpath,f.Name,f.Extension);
                    allAssets.Add(path, info);
                }
                EditorUtility.DisplayProgressBar(curRootAsset, path, curProgress);
                CreateDeps(info, self);
            }
            EditorUtility.UnloadUnusedAssetsImmediate();
        }
    }
}
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
            CreatAllAsset(Application.dataPath);
            var bundlesNames = AssetDatabase.GetAllAssetBundleNames();
            for (int i = 0; i < bundlesNames.Length; i++)
            {
                string name = bundlesNames[i];
                AssetBundleInfo ab = new AssetBundleInfo(name);
                var assets = AssetDatabase.GetAssetPathsFromAssetBundle(name);
                for (int j = 0; j < assets.Length; j++)
                {
                    string fullpath = AssetTool.AssetPath2FullPath(assets[j]);
                    FileInfo f = new FileInfo(fullpath);
                    Asset_Bundle asset = new Asset_Bundle(fullpath, f.Name, f.Extension);
                    ab.AddAsset(asset);
                }
                assetBundles.Add(ab);
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
                curProgress = (float)ind / (float)fs.Length;
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
                //标记一下是文件夹下根资源
                CreateDeps(info);
            }
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
            AssetDatabase.SaveAssets();
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
                string fullpath = AssetTool.AssetPath2FullPath(assetpath);
                FileInfo f = new FileInfo(fullpath);
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
                    info = new Asset_Bundle(fullpath,f.Name,f.Extension);
                    allAssets.Add(assetpath, info);
                }
                EditorUtility.DisplayProgressBar(curRootAsset, assetpath, curProgress);
                CreateDeps(info, self);
            }
        }
    }
}
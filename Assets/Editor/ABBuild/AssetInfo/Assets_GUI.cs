using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ABBuild
{
    public class Assets_GUI
    {
        public Asset_GUI rootAsset;
        public Assets_GUI()
        {
            //以Assets目录创建根对象
            rootAsset = new Asset_GUI(Application.dataPath, "Assets", true);
            //从根对象开始，读取所有文件创建子对
            AssetTool.ReadAssetsInChildren(rootAsset);
            Resources.UnloadUnusedAssets();
        }

        public AssetBase GetAssetInfo(string assetPath)
        {
            int index = assetPath.IndexOf(Path.AltDirectorySeparatorChar);
            if (index < 0||index>=assetPath.Length)
            {
                return rootAsset;
            }
            else
            {
                var newpath = assetPath.Substring(index + 1);
                return GetAssetInfo(newpath, rootAsset);
            }
        }

        private Asset_GUI GetAssetInfo(string path, Asset_GUI assetGui)
        {
            if (string.IsNullOrEmpty(path))
            {
                return assetGui;
            }

            int index = path.IndexOf(Path.AltDirectorySeparatorChar);
            string name = null;
            if (index < 0)
            {
                //最后一个
                name = path;
                return assetGui.GetChild_GUI(name);
            }
            else
            {
                //不是最后一个
                name = path.Substring(0, index);
                var newAssetInfo = assetGui.GetChild_GUI(name);
                var newpath = path.Substring(index + 1);
                return GetAssetInfo(newpath, newAssetInfo);
            }
        }

        public void SetBundled(Dictionary<string, Dictionary<string, AssetBundleInfo>> assetBundleBundlesDic)
        {
            foreach (var bundlednames in assetBundleBundlesDic)
            {
                foreach (var bundleInfo in bundlednames.Value)
                {
                    foreach (var asset in bundleInfo.Value.assets)
                    {
                        string assetPath = asset.assetPath;
                        var asset_gui = GetAssetInfo(assetPath);
                        asset_gui.bundled = bundlednames.Key;
                    }
                }
            }
        }
    }
}
﻿using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ABBuild
{
    public static class AssetBundleTool
    {
        /// <summary>
        /// 清空AB包中的资源
        /// </summary>
        public static void ClearAsset(this AssetBundleInfo build)
        {
            for (int i = 0; i < build.assets.Count; i++)
            {
                build.assets[i].bundled = "";
                AssetImporter import = AssetImporter.GetAtPath(build.assets[i].assetPath);
                import.assetBundleName = "";
            }
            build.assets.Clear();
        }
        /// <summary>
        /// 删除AB包
        /// </summary>
        public static void DeleteAssetBundle(this AssetBundleInfos abInfo, int index)
        {
            abInfo.assetBundles[index].ClearAsset();
            AssetDatabase.RemoveAssetBundleName(abInfo.assetBundles[index].name, true);
            abInfo.assetBundles.RemoveAt(index);
        }
        /// <summary>
        ///打包
        /// </summary>
        /// <param name="outPath"></param>
        /// <param name="options"></param>
        /// <param name="buildTarget"></param>
        /// <returns></returns>
        public static AssetBundleManifest BuildAssetBundles(string outPath,BuildAssetBundleOptions options,BuildTarget buildTarget)
        {
            Debug.Log("开始打包");
            var manifest = BuildPipeline.BuildAssetBundles(outPath, options, buildTarget);
            return manifest;
        }
    }
}
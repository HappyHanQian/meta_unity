using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ABBuild
{
    public static class AssetBundleTool
    {
        //自定义规则文件路径
        public static string customPath=Path.Combine(Application.dataPath, "Editor", "ABBuild","Temp", "custom_rule");
        /// <summary>
        /// 打包粒度
        /// </summary>
        public static int[] pieceThreshold = new[] {0, 2, 5, 10, 50,100};
        /// <summary>
        /// 自定义bundle规则
        /// </summary>
        public static Dictionary<string, string> custom_rule=new Dictionary<string, string>();

        /// <summary>
        /// 清空AB包中的资源
        /// </summary>
        public static void ClearAsset(this AssetBundleInfo build)
        {
            for (int i = 0; i < build.assets.Count; i++)
            {
                build.assets[i].bundled = "";
                build.assets[i].variant = "";
            }
            build.assets.Clear();
        }

        /// <summary>
        /// 删除AB包
        /// </summary>
        public static void DeleteAssetBundle(this AssetBundleInfos abInfo, string abname, string variant)
        {
            abInfo.bundlesDic[abname][variant].ClearAsset();
            abInfo.bundlesDic.Remove(abname);
        }

        /// <summary>
        /// 根据扩展名判断是否是一个有效的bundle资源
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool isValidBundleAsset(FileInfo f)
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

        /// <summary>
        ///打包
        /// </summary>
        /// <param name="outPath"></param>
        /// <param name="options"></param>
        /// <param name="buildTarget"></param>
        /// <returns></returns>
        public static AssetBundleManifest BuildAssetBundles(AssetBundleInfos assetBundleInfos, string outPath,
            BuildAssetBundleOptions options, BuildTarget buildTarget)
        {
            Debug.Log("开始打包");
            var bundles = assetBundleInfos.GetAssetBundleBuildInfo();
            string path = Path.Combine(outPath, buildTarget.ToString());
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var manifest = BuildPipeline.BuildAssetBundles(path, bundles, options, buildTarget);
            CreaBuildFile(path, assetBundleInfos, manifest);
            return manifest;
        }

        /// <summary>
        /// 创建build文件(bundlelist和bundleinfo)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="assetBundleInfos"></param>
        /// <param name="manifest"></param>
        public static void CreaBuildFile(string path, AssetBundleInfos assetBundleInfos, AssetBundleManifest manifest)
        {
            manifest.GetAllAssetBundles();
            string handlepath = Path.Combine(path, "BundleList");
            Dictionary<string, Dictionary<string, int>> bundle_ids = new Dictionary<string, Dictionary<string, int>>();
            StringBuilder sb = new StringBuilder();
            var keys = assetBundleInfos.bundlesDic.Keys.ToArray();
            int id = 0;
            AddManifest(path, ref id, ref sb);
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var variantKeys = assetBundleInfos.bundlesDic[key].Keys.ToArray();
                for (int j = 0; j < variantKeys.Length; j++)
                {
                    var variant_key = variantKeys[j];
                    string temp =
                        $"{id}|{key}|{variant_key}|{GetFileMD5(Path.Combine(path, key))}"; //id|bundeNam|bundleVariant|md5
                    sb.AppendLine(temp);
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

            CreatFile(handlepath, sb.ToString());
            CreatBundleInfoFile(path, assetBundleInfos, bundle_ids);
        }

        private static void AddManifest(string path, ref int id, ref StringBuilder sb)
        {
            FileInfo file = new FileInfo(path);
            string temp = $"{id}|{file.Name}|{string.Empty}|{GetFileMD5(Path.Combine(path, file.Name))}";
            sb.AppendLine(temp);
            id++;
        }

        public static void CreatBundleInfoFile(string path, AssetBundleInfos assetBundleInfos,
            Dictionary<string, Dictionary<string, int>> bundleDic)
        {
            string handlepath = Path.Combine(path, "BundldInfo");
            StringBuilder sb = new StringBuilder();
            var keys = assetBundleInfos.allAssets.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                var asset = assetBundleInfos.allAssets[keys[i]];
                string temp = $"{asset.assetName}|{bundleDic[asset.bundled][asset.variant]}";
                sb.AppendLine(temp);
            }

            CreatFile(handlepath, sb.ToString());
        }

        public static string GetFileMD5(string filePath)
        {
            try
            {
                FileStream file = new FileStream(filePath, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return "";
            }
        }

        public static void CreatFile(string path, string content)
        {
            byte[] myBytes = Encoding.UTF8.GetBytes(content);
            using (FileStream file = new FileStream(path, FileMode.OpenOrCreate))
            {
                file.Write(myBytes, 0, myBytes.Length);
            }
        }

        public static void ReadCustom()
        {
            if (string.IsNullOrEmpty(customPath))
            {
                Debug.LogError("build path is null or empty");
                return;
            }
            custom_rule.Clear();
            if (!File.Exists(customPath))
            {
                return;
            }
            else
            {
                var e = File.ReadLines(customPath).GetEnumerator();
                while (e.MoveNext())
                {
                    string[] temp = e.Current.Split('|');
                    custom_rule.Add(temp[0], temp[1]);
                }
            }
        }

        public static void ChangeCustomRule(string assetName, string bundleName)
        {
            if (custom_rule.Count<=0)
            {
                ReadCustom();
            }
            custom_rule[assetName] = bundleName;
            WriteCustom();
        }

        public static void RemoveCustomRule(string assetName)
        {
            if (custom_rule.Count <= 0)
            {
                ReadCustom();
            }
            if (custom_rule.ContainsKey(assetName))
            {
                custom_rule.Remove(assetName);
            }
            WriteCustom();
        }
        public static void WriteCustom()
        {
            if (string.IsNullOrEmpty(customPath))
            {
                Debug.LogError("build path is null or empty");
                return;
            }

            if (custom_rule.Count<=0&&File.Exists(customPath))
            {
                File.Delete(customPath);
                return;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                var keys = custom_rule.Keys.ToArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    sb.AppendLine($"{key}|{custom_rule[key]}");
                }
                CreatFile("", sb.ToString());
            }
        }
    }
}
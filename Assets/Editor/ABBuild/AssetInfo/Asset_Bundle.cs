using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ABBuild
{
    /// <summary>
    /// 打ab包得asset数据
    /// </summary>
    public class Asset_Bundle:AssetBase
    {
        /// <summary>
        /// 引用该资源的资源
        /// </summary>
        public HashSet<Asset_Bundle> parents;
        /// <summary>
        /// 该资源引用的资源
        /// </summary>
        public HashSet<Asset_Bundle> childs;

        /// <summary>
        /// 构造需要打包的资源结构
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="name"></param>
        /// <param name="extension"></param>
        public Asset_Bundle(string fullPath, string name, string extension):base(fullPath,name,extension)
        {
            this.parents = new HashSet<Asset_Bundle>();
            this.childs = new HashSet<Asset_Bundle>();
        }

        public bool HasParent(Asset_Bundle parent)
        {
            return parents.Contains(parent);
        }

        public void AddParent(Asset_Bundle parent)
        {
            this.parents.Add(parent);
        }

        public Object GetAsset()
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            return asset;
        }

        public void SetAssetBundleName(int pieceThreshold)
        {
            AssetImporter ai = AssetImporter.GetAtPath(this.assetPath);
            //针对UGUI图集的处理,图集以文件夹为单位打包ab
            if (ai is TextureImporter)
            {
                TextureImporter tai = ai as TextureImporter;

                string filePath = System.IO.Path.GetDirectoryName(this.assetPath);
                tai.spritePackingTag = filePath.ToLower().Replace("\\", "_").Replace(".png", string.Empty).Replace(".jpg", string.Empty).Replace(" ", string.Empty);

                //AssetBundleName和spritePackingTag保持一致
                tai.SetAssetBundleNameAndVariant(tai.spritePackingTag + ".ab", null);
                Debug.Log("<color=#2E8A00>" + "设置ab，Image资源: " + this.assetPath + "</color>");
            }
            else
            {
                string abname = this.assetPath.Replace("/", "_") + ".ab";
                //不是图集，而且大于阀值
                if (this.parents.Count >= pieceThreshold)
                {
                    ai.SetAssetBundleNameAndVariant(abname, string.Empty);
                    Debug.Log("<color=#6501AB>" + "设置ab，有多个引用: " + this.assetPath + "</color>");
                }
                //根节点
                else if (this.parents.Count == 0)
                {
                    ai.SetAssetBundleNameAndVariant(abname, string.Empty);
                    Debug.Log("<color=#025082>" + "设置ab，根资源ab: " + this.assetPath + "</color>");
                }
                else
                {
                    //其余的子资源
                    ai.SetAssetBundleNameAndVariant(string.Empty, string.Empty);
                    Debug.Log("<color=#DBAF00>" + "清除ab， 仅有1个引用: " + this.assetPath + "</color>");
                }
            }
        }
    }
}
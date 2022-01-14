﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ABBuild
{
    /// <summary>
    /// 打ab包得asset数据
    /// </summary>
    public class Asset_Bundle : AssetBase
    {
        public string extension;

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
        public Asset_Bundle(string fullPath, string name, string extension) : base(fullPath, name, extension)
        {
            this.extension = extension;
            this.parents = new HashSet<Asset_Bundle>();
            this.childs = new HashSet<Asset_Bundle>();
        }

        public bool HasParent(Asset_Bundle parent)
        {
            return parents.Contains(parent);
        }

        public void AddParent(Asset_Bundle parent)
        {
            if (parent == this || IsParentEarlyDep(parent) || parent == null)
                return;

            parents.Add(parent);
            parent.AddChild(this);

            parent.RemoveRepeatChildDep(this);
            RemoveRepeatParentDep(parent);
        }

        private void AddChild(Asset_Bundle child)
        {
            childs.Add(child);
        }

        /// <summary>
        /// 清除我父节点对我子节点的重复引用，保证树形结构
        /// </summary>
        /// <param name="targetParent"></param>
        private void RemoveRepeatChildDep(Asset_Bundle targetChild)
        {
            List<Asset_Bundle> infolist = new List<Asset_Bundle>(parents);
            for (int i = 0; i < infolist.Count; i++)
            {
                Asset_Bundle pinfo = infolist[i];
                pinfo.RemoveChild(targetChild);
                pinfo.RemoveRepeatChildDep(targetChild);
            }
        }

        /// <summary>
        /// 清除我子节点被我父节点的重复引用，保证树形结构
        /// </summary>
        /// <param name="targetChild"></param>
        private void RemoveRepeatParentDep(Asset_Bundle targetParent)
        {
            List<Asset_Bundle> infolist = new List<Asset_Bundle>(childs);
            for (int i = 0; i < infolist.Count; i++)
            {
                Asset_Bundle cinfo = infolist[i];
                cinfo.RemoveParent(targetParent);
                cinfo.RemoveRepeatParentDep(targetParent);
            }
        }

        private void RemoveChild(Asset_Bundle targetChild)
        {
            childs.Remove(targetChild);
            targetChild.parents.Remove(this);
        }

        private void RemoveParent(Asset_Bundle parent)
        {
            parent.childs.Remove(this);
            parents.Remove(parent);
        }

        /// <summary>
        /// 如果父节点早已当此父节点为父节点
        /// </summary>
        /// <param name="targetParent"></param>
        /// <returns></returns>
        private bool IsParentEarlyDep(Asset_Bundle targetParent)
        {
            if (parents.Contains(targetParent))
            {
                return true;
            }

            var e = parents.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.IsParentEarlyDep(targetParent))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetAssetBundleNameAndVariant(string abname, string variant)
        {
            AssetImporter ai = AssetImporter.GetAtPath(this.assetPath);
            ai.SetAssetBundleNameAndVariant(abname,variant);
        }
        public void SetAssetBundleName(int pieceThreshold)
        {
            AssetImporter ai = AssetImporter.GetAtPath(this.assetPath);
            string abname = this.assetPath.Replace("/", "_") + ".ab";

            if (this.extension == ".spriteatlas")
            {
                //是图集
                ai.SetAssetBundleNameAndVariant(abname, string.Empty);
                foreach (var child in this.childs)
                {
                    child.SetAssetBundleNameAndVariant(abname, string.Empty);
                }
                return;
            }
            else
            {
                
            }
            if (ai is TextureImporter)
            {
                //是图片资源
                bool isInSptrite = false;
                foreach (var parent in parents)
                {
                    if (parent.extension == ".spriteatlas")
                    {
                        isInSptrite = true;
                        break;
                    }
                }

                if (isInSptrite)
                {
                    //在图集里
                    return;
                }

                // TextureImporter tai = ai as TextureImporter;
                // string filePath = System.IO.Path.GetDirectoryName(this.assetPath);
                // tai.spritePackingTag = filePath.ToLower().Replace("\\", "_").Replace(".png", string.Empty).Replace(".jpg", string.Empty).Replace(" ", string.Empty);
                // //AssetBundleName和spritePackingTag保持一致
                // tai.SetAssetBundleNameAndVariant(tai.spritePackingTag + ".ab", null);
                // Debug.Log("<color=#2E8A00>" + "设置ab，Image资源: " + this.assetPath + "</color>");
            }
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
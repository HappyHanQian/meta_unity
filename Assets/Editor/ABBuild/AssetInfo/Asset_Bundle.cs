using System.Collections.Generic;
using System.IO;
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

        public string variant;
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
            this.variant = string.Empty;
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

        public void SetAssetBundleNameAndVariant(string abname=null, string variant=null)
        {
            abname=string.IsNullOrEmpty(abname)?GetAbName():abname;
            variant = string.IsNullOrEmpty(variant) ? GetAbVariant() : variant;
            this.bundled = abname;
            this.variant = variant;
        }

        public string GetAbName()
        {
            if (!string.IsNullOrEmpty(this.bundled))
            {
                return this.bundled;
            }
            else
            {
                if (this.extension== ".spriteatlas")
                {
                    //是图集
                    string abname = "0#" + this.assetPath.Replace("/", "_").Replace(this.extension, "");
                    return abname;
                }
                switch (this.parents.Count)
                {
                    // case 0:
                    //     return this.assetPath.Replace("/", "_") + ".ab";
                    case 1:
                        var e = parents.GetEnumerator();
                        e.MoveNext();
                        return e.Current.GetAbName();
                    default:
                        int count = this.parents.Count;
                        int min = AssetBundleTool.pieceThreshold[0];
                        string dir_name = Path.GetDirectoryName(this.assetPath);
                        for (int i = 1; i < AssetBundleTool.pieceThreshold.Length; i++)
                        {
                            int cur = AssetBundleTool.pieceThreshold[i];
                            if (count<cur)
                            {
                                return min + "#" + dir_name.Replace("/", "_").Replace("\\", "_");
                            }
                            else
                            {
                                min = cur;
                            }
                        }
                        return min + "#" + dir_name.Replace("/", "_").Replace("\\", "_");
                }
            }
        }
        public string GetAbVariant()
        {
            if (!string.IsNullOrEmpty(this.variant))
            {
                return this.variant;
            }
            else
            {
                if (this.extension== ".spriteatlas")
                {
                    return string.Empty;
                }
                if (this.parents.Count == 1) 
                {
                    var e = parents.GetEnumerator();
                    e.MoveNext();
                    return e.Current.GetAbVariant();
                }
                else
                {
                    return this.variant;
                }
            }
        }
        public void SetAssetBundleNameBuRule()
        {
            if (this.extension == ".spriteatlas")
            {
                //是图集
                SetAssetBundleNameAndVariant();
                foreach (var child in this.childs)
                {
                    child.SetAssetBundleNameAndVariant(GetAbName(), GetAbVariant());
                }
                return;
            }
            else
            {
                
            }
            if (this.extension==".png"||this.extension==".jpg")
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
            SetAssetBundleNameAndVariant();
            // //不是图集，而且大于阀值
            // if (this.parents.Count >= pieceThreshold)
            // {
            //     SetAssetBundleNameAndVariant();
            //     Debug.Log("<color=#6501AB>" + "设置ab，有多个引用: " + this.assetPath + "</color>");
            // }
            // //根节点
            // else if (this.parents.Count == 0)
            // {
            //     SetAssetBundleNameAndVariant();
            //     Debug.Log("<color=#025082>" + "设置ab，根资源ab: " + this.assetPath + "</color>");
            // }
            // else
            // {
            //     //其余的子资源
            //     
            //     Debug.Log("<color=#DBAF00>" + "清除ab， 仅有1个引用: " + this.assetPath + "</color>");
            // }
        }
    }
}
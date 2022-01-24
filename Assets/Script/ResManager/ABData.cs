using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Script.ResManager
{
    public class ABData
    {
        /// <summary>
        /// 被依赖引用计数(显性加载的bundle不进行计数)
        /// </summary>
        private int used = 0;
        /// <summary>
        /// 删除时间(-1为常驻不删除)
        /// </summary>
        public float unLoadTime;
        public AssetBundle ab;
        public Dictionary<string, System.WeakReference> allAssets;
        public ABData(AssetBundle assetBundle, float unLoadTime)
        {
            this.ab = assetBundle;
            this.unLoadTime = unLoadTime;
            used = 1;
            allAssets = new Dictionary<string,System.WeakReference>();
        }
        public T LoadAsset<T>(string assetName)where T:Object
        {
            if (allAssets.ContainsKey(assetName)&&allAssets[assetName].Target!=null)
            {
                return (T)allAssets[assetName].Target;
            }
            else
            {
                T asset = ab.LoadAsset<T>(assetName);
                if (asset!=null)
                {
                    allAssets[assetName] = new WeakReference(asset);
                }
                return asset;
            }
        }

        public bool CanUnLoad()
        {
            if (allAssets.Count==0)
            {
                return false;
            }

            bool canUnLoad = true;
            foreach (var asset in allAssets)
            {
                if (IsActivie(asset.Value))
                {
                    canUnLoad = false;
                    break;
                }
            }

            return canUnLoad&&used<=0;
        }

        public bool IsActivie(System.WeakReference obj)
        {
            if (obj.Target==null)
            {
                return false;
            }
            else
            {
                return obj.IsAlive;
            }
        }

        public void Use()
        {
            used++;
        }

        public void UnUse()
        {
            used--;
        }
        public void UnLoad(bool unloadallLoadAsset)
        {
            if (ab!=null)
            {
                ab.Unload(unloadallLoadAsset);
                ab = null;
            }
            if (unloadallLoadAsset)
            {
                allAssets.Clear();
            }
        }
    }
}
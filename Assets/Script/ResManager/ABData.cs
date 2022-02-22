using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class ABData : ObjeckBase
{
    /// <summary>
    /// 被依赖引用计数(显性加载的bundle不进行计数)
    /// </summary>
    private int used = 0;

    /// <summary>
    /// 删除时间(-1为常驻不删除)
    /// </summary>
    private float unLoadTime;

    public AssetBundle ab;

    /// <summary>
    /// 已经加载的资源列表
    /// </summary>
    public Dictionary<string, System.WeakReference> allAssets;

    public List<string> loadingList;

    public ABData()
    {
        used = 1;
        allAssets = new Dictionary<string, System.WeakReference>();
        loadingList = new List<string>();
    }

    public void SetAssetBundle(AssetBundle assetBundle)
    {
        this.ab = assetBundle;
    }

    public void SetUnLoadTime(float unLoadTime)
    {
        this.unLoadTime = unLoadTime;
    }

    public float GetUnLoadTime()
    {
        return this.unLoadTime;
    }

    public T LoadAsset<T>(string assetName) where T : Object
    {
        if (allAssets.ContainsKey(assetName) && allAssets[assetName].Target != null)
        {
            return (T) allAssets[assetName].Target;
        }
        else
        {
            T asset = ab.LoadAsset<T>(assetName);
            if (asset != null)
            {
                allAssets[assetName] = new WeakReference(asset);
            }

            return asset;
        }
    }

    public IEnumerator LoadAssetAsync<T>(string assetName, Action<T> callBack) where T : Object
    {
        if (allAssets.ContainsKey(assetName) && allAssets[assetName].Target != null)
        {
            callBack?.Invoke((T) allAssets[assetName].Target);
        }
        else
        {
            if (loadingList.Contains(assetName))
            {
                yield return new WaitUntil(delegate() { return !loadingList.Contains(assetName); });
                if (allAssets.ContainsKey(assetName) && allAssets[assetName].Target != null)
                {
                    callBack?.Invoke((T) allAssets[assetName].Target);
                }
            }
            else
            {
                loadingList.Add(assetName);
                var loader = ab.LoadAssetAsync(assetName);
                yield return loader;
                if (loader.asset != null)
                {
                    allAssets[assetName] = new WeakReference(loader.asset);
                    callBack?.Invoke(loader.asset as T);
                }

                loadingList.Remove(assetName);
            }
        }
    }

    public bool CanUnLoad()
    {
        if (allAssets.Count == 0 || loadingList.Count > 0)
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

        return canUnLoad && used <= 0;
    }

    public bool IsActivie(System.WeakReference obj)
    {
        if (obj.Target == null)
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
        if (ab != null)
        {
            ab.Unload(unloadallLoadAsset);
            ab = null;
        }

        if (unloadallLoadAsset)
        {
            allAssets.Clear();
        }
    }

    public override void ReSet()
    {
        used = 0;
        unLoadTime = 0;
        ab = null;
        allAssets.Clear();
        loadingList.Clear();
    }
}
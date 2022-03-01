using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

public class Loader_Bundle : ResLoader, ResLoader_Stop
{
    private string rootPath;

    /// <summary>
    /// bundle信息,id,name,md5
    /// </summary>
    public BundleList bundleList;

    /// <summary>
    /// bundle信息 资源对应的bundleid
    /// </summary>
    public BundleInfo bundleInfo;

    /// <summary>
    /// 可以获取bundle包的依赖关系
    /// </summary>
    private AssetBundleManifest manifest;

    public Dictionary<string, ABData> abMap;

    /// <summary>
    /// 要卸载的bundle列表
    /// </summary>
    private List<string> unloadList;

    /// <summary>
    /// 正在进行异步加载的bundle列表
    /// </summary>
    private List<string> loadingList;

    public void Init(string path)
    {
        rootPath = path;
        abMap = new Dictionary<string, ABData>();
        unloadList = new List<string>();
        loadingList = new List<string>();
        ReadBundleList();
        ReadBundleInfo();
        LoadManifest();
        GameMain.Inst.StartCoroutine(CheckBundle());
    }
    private string GetLocalFilePath(string fileName)
    {
        string result = Path.Combine(rootPath, fileName);
        if (File.Exists(result))
        {
            return result;
        }

        result = Path.Combine(Application.streamingAssetsPath, fileName);
        Debug.LogError(result);
        return result;
    }

    private void LoadManifest()
    {
        string bundleName = bundleList.bundles[0].name;
        string path = GetLocalFilePath(bundleName);
        var main = AssetBundle.LoadFromFile(path);
        if (main == null)
        {
            Debug.LogError("manifest file is not exit");
        }
        else
        {
            manifest = main.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            main.Unload(false);
        }
    }

    private void ReadBundleInfo()
    {
        if (bundleInfo!=null)
        {
            return;
        }
        bundleInfo = new BundleInfo();
        bundleInfo.bundleInfos = new Dictionary<string, int>();
        string bundleName = bundleList.bundles[bundleList.bundles.Count - 1].name;
        string bundleInfoPath = GetLocalFilePath(bundleName);
        var main = AssetBundle.LoadFromFile(bundleInfoPath);
        if (main != null)
        {
            var asset = main.LoadAsset<TextAsset>("BundleInfo");
            StringReader strReader = new StringReader(asset.text);
            string aLine = null;
            while (true)
            {
                aLine = strReader.ReadLine();
                if (aLine != null)
                {
                    var aLines = aLine.Split('|');
                    if (aLines.Length >= 2)
                    {
                        bundleInfo.bundleInfos[aLines[0]] = int.Parse(aLines[1]);
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    private void ReadBundleList()
    {
        if (bundleList!=null)
        {
            return;
        }
        bundleList = new BundleList();
        bundleList.bundles = new Dictionary<int, BundleData>();
        string bundleListPath = GetLocalFilePath("BundleList");
        string text = FileTool.LoadTxtFileByWWW(bundleListPath);
        StringReader strReader = new StringReader(text);
        string aLine = null;
        while (true)
        {
            aLine = strReader.ReadLine();
            if (aLine != null)
            {
                var aLines = aLine.Split('|');
                if (aLines.Length==1)
                {
                    //版本信息
                    bundleList.version = aLine;
                    continue;
                }
                
                if (aLines.Length >= 3)
                {
                    int id = int.Parse(aLines[0]);
                    string bundlename = aLines[1];
                    string md5 = aLines[2];
                    BundleData bd = new BundleData();
                    bd.id = id;
                    bd.name = bundlename;
                    bd.md5 = md5;
                    bundleList.bundles.Add(id, bd);
                }
            }
            else
            {
                break;
            }
        }
    }

    public T LoadAsset<T>(string assetName) where T : Object
    {
        string bundleName = GetBundleName(assetName);
        if (string.IsNullOrEmpty(bundleName))
        {
            Debug.LogError($"bundle中没有该资源:{assetName}");
            return null;
        }
        else
        {
            if (abMap.ContainsKey(bundleName))
            {
                ABData ab = abMap[bundleName];
                AddToUnloadList(bundleName);
                return ab.LoadAsset<T>(assetName);
            }
            else
            {
                var ab = LoadAssetBundleWithDencies(bundleName);
                if (ab != null)
                {
                    AddToUnloadList(bundleName);
                    return ab.LoadAsset<T>(assetName);
                }
                else
                {
                    return null;
                }
            }
        }
    }


    private ABData LoadAssetBundleWithDencies(string bundleName)
    {
        var bundles = manifest.GetAllDependencies(bundleName);
        for (int i = 0; i < bundles.Length; i++)
        {
            var bn = bundles[i];
            if (abMap.ContainsKey(bn))
            {
                abMap[bn].Use();
                continue;
            }

            LoadAssetBundle(bn, true);
        }

        var ab = LoadAssetBundle(bundleName, false);
        return ab;
    }

    /// <summary>
    /// 加载bundle
    /// </summary>
    /// <param name="bundleName">bundleName</param>
    /// <param name="useNum">是否需要计数(被依赖的包需要计数)</param>
    /// <returns></returns>
    private ABData LoadAssetBundle(string bundleName, bool useNum)
    {
        var path = GetLocalFilePath(bundleName);
        var assetbundle = AssetBundle.LoadFromFile(path);
        if (assetbundle == null)
        {
            Debug.LogError($"{bundleName} file is not exit");
            return null;
        }

        ABData ab = ObjectPoolManager.Inst.GetObject<ABData>();
        ab.SetAssetBundle(assetbundle);
        abMap.Add(bundleName, ab);

        if (useNum)
        {
            ab.Use();
        }

        return ab;
    }

    #region 异步加载

    public IEnumerator LoadAssetAsync<T>(string assetName, Action<T> callBack) where T : Object
    {
        yield return new WaitForSecondsRealtime(1);
        string bundleName = GetBundleName(assetName);
        if (string.IsNullOrEmpty(bundleName))
        {
            Debug.LogError($"bundle中没有该资源:{assetName}");
        }
        else
        {
            if (!abMap.ContainsKey(bundleName))
            {
                //没有下载过
                if (loadingList.Contains(bundleName))
                {
                    //在下载列表里
                    yield return new WaitUntil(delegate() { return !loadingList.Contains(bundleName); });
                }
                else
                {
                    //没有在下载列表里
                    yield return LoadAssetBundleWithDenciesAsync(bundleName);
                }
            }
            else
            {
                //已经存在在map中了,直接从map中拿
            }

            if (abMap.ContainsKey(bundleName))
            {
                ABData ab = abMap[bundleName];
                yield return ab.LoadAssetAsync(assetName, callBack);
                //加入卸载列表
                AddToUnloadList(bundleName);
            }
            else
            {
                callBack?.Invoke(null);
            }
        }
    }

    private void AddToUnloadList(string bundleName)
    {
        var bundles = manifest.GetAllDependencies(bundleName);
        for (int i = 0; i < bundles.Length; i++)
        {
            var n = bundles[i];
            var t = GetUnLoadTime(n);
            if (t > 0 && !unloadList.Contains(bundleName))
            {
                if (!unloadList.Contains(n))
                {
                    unloadList.Add(n);
                }

                abMap[n].SetUnLoadTime(t);
            }
        }

        float unLoadTime = GetUnLoadTime(bundleName);
        if (unLoadTime > 0 && !unloadList.Contains(bundleName))
        {
            if (!unloadList.Contains(bundleName))
            {
                unloadList.Add(bundleName);
            }

            abMap[bundleName].SetUnLoadTime(unLoadTime);
        }
    }

    private IEnumerator LoadAssetBundleWithDenciesAsync(string bundleName)
    {
        var bundles = manifest.GetAllDependencies(bundleName);
        for (int i = 0; i < bundles.Length; i++)
        {
            var bn = bundles[i];
            if (abMap.ContainsKey(bn))
            {
                abMap[bn].Use();
                continue;
            }

            yield return LoadAssetBundleAsync(bn, true);
        }

        yield return LoadAssetBundleAsync(bundleName, false);
    }

    private IEnumerator LoadAssetBundleAsync(string bundleName, bool useNum)
    {
        if (loadingList.Contains(bundleName))
        {
            yield return new WaitUntil(delegate() { return !loadingList.Contains(bundleName); });
        }


        if (abMap.ContainsKey(bundleName))
        {
        }
        else
        {
            loadingList.Add(bundleName);
            string path = GetLocalFilePath(bundleName);
            var bundleLoad = AssetBundle.LoadFromFileAsync(path);
            if (bundleLoad == null)
            {
                Debug.LogError($"{bundleName} file is not exit");
            }

            yield return bundleLoad;
            if (bundleLoad.assetBundle != null)
            {
                ABData ab = ObjectPoolManager.Inst.GetObject<ABData>();
                ab.SetAssetBundle(bundleLoad.assetBundle);
                if (useNum)
                {
                    ab.Use();
                }

                abMap.Add(bundleName, ab);
            }

            loadingList.Remove(bundleName);
        }
    }

    public void StopAllLoad()
    {
        loadingList.Clear();
    }

    public void StopLoad(string assetName)
    {
        string bundleName = GetBundleName(assetName);
        if (loadingList.Contains(bundleName))
        {
            loadingList.Remove(bundleName);
        }
    }

    #endregion

    public float GetUnLoadTime(string bundlename)
    {
        int index = bundlename.IndexOf('#');
        if (index < 0)
        {
            //没有被依赖计数
            return Time.realtimeSinceStartup + 60;
        }
        else
        {
            int times = int.Parse(bundlename.Substring(0, index));
            if (times >= 100)
            {
                //被依赖的bundle包个数大于等于100
                //不会根据时间策略卸载
                return -1;
            }
            else
            {
                return Time.realtimeSinceStartup + (times + 1) * 10;
            }
        }
    }

    IEnumerator CheckBundle()
    {
        while (true)
        {
            var curTime = Time.realtimeSinceStartup;
            for (int i = unloadList.Count - 1; i >= 0; i--)
            {
                string bundlename = unloadList[i];
                if (abMap[bundlename].CanUnLoad())
                {
                    //60秒后删除
                    abMap[bundlename].SetUnLoadTime(curTime + 60);
                }

                if (abMap[bundlename].GetUnLoadTime() >= 0 && curTime >= abMap[bundlename].GetUnLoadTime())
                {
                    PreUnloadBundle(bundlename);
                    abMap[bundlename].UnLoad(false);
                    ObjectPoolManager.Inst.Recycle(abMap[bundlename]);
                    abMap.Remove(bundlename);
                    unloadList.Remove(bundlename);
                }
            }

            yield return null;
        }
    }

    private void PreUnloadBundle(string bundleName)
    {
        var bundles = manifest.GetAllDependencies(bundleName);
        for (int i = 0; i < bundles.Length; i++)
        {
            var temp = bundles[i];
            if (abMap.ContainsKey(temp))
            {
                abMap[temp].UnUse();
            }
            else
            {
                Debug.Log($"abMap 中没有该ab包:{bundleName}");
            }
        }
    }

    private string GetBundleName(string assetName)
    {
        int id = -1;
        if (bundleInfo.bundleInfos.ContainsKey(assetName))
        {
            id = bundleInfo.bundleInfos[assetName];
        }

        if (bundleList.bundles.ContainsKey(id))
        {
            return bundleList.bundles[id].name;
        }

        return "";
    }
}

public class BundleList
{
    public string version;
    public Dictionary<int, BundleData> bundles;
}

public struct BundleData
{
    public int id;
    public string name;
    public string md5;
}

public class BundleInfo
{
    public Dictionary<string, int> bundleInfos;
}
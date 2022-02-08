using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Script.Tool;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Script.ResManager
{
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

        private Dictionary<string, ABData> abMap;

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
            bool isCorrect = IsBundleList_Correct();
            if (isCorrect)
            {
                LoadManifest();
                GameMain.Inst.StartCoroutine(CheckBundle());
            }
            else
            {
                Debug.LogError("bundle version is error");
                //应该强制退出游戏
            }
        }

        private bool IsBundleList_Correct()
        {
            if (bundleInfo == null || bundleList == null)
            {
                return false;
            }

            return bundleList.version == bundleInfo.version;
        }

        private void LoadManifest()
        {
            string bundleName = bundleList.bundles[0].name;
            string path = Path.Combine(rootPath, bundleName);
            var main = AssetBundle.LoadFromFile(path);
            if (main != null)
            {
                manifest = main.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                main.Unload(false);
            }
            else
            {
                Debug.LogError("manifest file is not exit");
            }
        }

        private void ReadBundleInfo()
        {
            bundleInfo = new BundleInfo();
            bundleInfo.bundleInfos = new Dictionary<string, int>();
            string bundleInfoPath = Path.Combine(rootPath, "BundldInfo");
            var e = File.ReadLines(bundleInfoPath).GetEnumerator();
            while (e.MoveNext())
            {
                string temp = e.Current;
                string[] temps = temp.Split('|');
                if (temps.Length == 1)
                {
                    //版本信息
                    bundleInfo.version = temp;
                }
                else if (temps.Length >= 2)
                {
                    string assetname = temps[0];
                    int id = int.Parse(temps[1]);
                    bundleInfo.bundleInfos.Add(assetname, id);
                }
            }
        }

        private void ReadBundleList()
        {
            bundleList = new BundleList();
            bundleList.bundles = new Dictionary<int, BundleData>();
            string bundleListPath = Path.Combine(rootPath, "BundleList");
            var e = File.ReadLines(bundleListPath).GetEnumerator();
            while (e.MoveNext())
            {
                string temp = e.Current;
                string[] temps = temp.Split('|');
                if (temps.Length == 1)
                {
                    //版本信息
                    bundleList.version = temp;
                }
                else if (temps.Length >= 3)
                {
                    int id = int.Parse(temps[0]);
                    string bundlename = temps[1];
                    string md5 = temps[2];
                    BundleData bd = new BundleData();
                    bd.id = id;
                    bd.name = bundlename;
                    bd.md5 = md5;
                    bundleList.bundles.Add(id, bd);
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
                    float unLoadTime = GetUnLoadTime(bundleName);
                    ABData ab = abMap[bundleName];
                    ab.unLoadTime = unLoadTime;
                    return ab.LoadAsset<T>(assetName);
                }
                else
                {
                    var ab = LoadAssetBundleWithDencies(bundleName);
                    return ab.LoadAsset<T>(assetName);
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
            float unLoadTime = GetUnLoadTime(bundleName);
            var assetbundle = AssetBundle.LoadFromFile(Path.Combine(rootPath, bundleName));
            ABData ab = new ABData(assetbundle, unLoadTime);
            abMap.Add(bundleName, ab);
            if (unLoadTime > 0 && !unloadList.Contains(bundleName))
            {
                unloadList.Add(bundleName);
            }

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
                    float unLoadTime = GetUnLoadTime(bundleName);
                    ABData ab = abMap[bundleName];
                    ab.unLoadTime = unLoadTime;
                    yield return ab.LoadAssetAsync(assetName, callBack);
                    if (unLoadTime > 0 && !unloadList.Contains(bundleName))
                    {
                        unloadList.Add(bundleName);
                    }

                }
                else
                {
                    callBack?.Invoke(null);
                }
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

            float unLoadTime = GetUnLoadTime(bundleName);

            if (abMap.ContainsKey(bundleName))
            {
                ABData ab = abMap[bundleName];
                ab.unLoadTime = unLoadTime;
            }
            else
            {
                loadingList.Add(bundleName);
                var bundleLoad = AssetBundle.LoadFromFileAsync(Path.Combine(rootPath, bundleName));
                yield return bundleLoad;
                if (bundleLoad.assetBundle != null)
                {
                    ABData ab = new ABData(bundleLoad.assetBundle, unLoadTime);
                    if (useNum)
                    {
                        if (unLoadTime > 0 && !unloadList.Contains(bundleName))
                        {
                            unloadList.Add(bundleName);
                        }
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
                //没有
                return Time.realtimeSinceStartup + 60;
            }
            else
            {
                int times = int.Parse(bundlename.Substring(0, index));
                if (times >= 100)
                {
                    return -1;
                }
                else
                {
                    return Time.realtimeSinceStartup + times * 10;
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
                        abMap[bundlename].unLoadTime = curTime + 60;
                    }
                
                    if (abMap[bundlename].unLoadTime>=0&&curTime >= abMap[bundlename].unLoadTime)
                    {
                        PreUnloadBundle(bundlename);
                        abMap[bundlename].UnLoad(false);
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
        public string version;
        public Dictionary<string, int> bundleInfos;
    }
}
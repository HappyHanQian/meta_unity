using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Script.Tool;
using UnityEngine;

namespace Assets.Script.ResManager
{
    public class Loader_Bundle : ResLoader
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

        private Dictionary<string, ABData> abs;
        private List<string> unloadList;

        public void Init(string path)
        {
            rootPath = path;
            abs = new Dictionary<string, ABData>();
            unloadList = new List<string>();
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
            if (bundleInfo==null||bundleList==null)
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

        public T Load<T>(string assetName) where T : Object
        {
            string bundleName = GetBundleName(assetName);
            if (string.IsNullOrEmpty(bundleName))
            {
                Debug.LogError($"bundle中没有该资源:{assetName}");
                return null;
            }
            else
            {
                if (abs.ContainsKey(bundleName))
                {
                    float unLoadTime = GetUnLoadTime(bundleName);
                    ABData ab = abs[bundleName];
                    ab.unLoadTime = unLoadTime;
                    return ab.LoadAsset<T>(assetName);
                }
                else
                {
                    var bundles = manifest.GetAllDependencies(bundleName);
                    for (int i = 0; i < bundles.Length; i++)
                    {
                        var bn = bundles[i];
                        if (abs.ContainsKey(bn))
                        {
                            abs[bn].Use();
                            continue;
                        }
                        LoadAssetBundle(bn,false);
                    }
                    var ab = LoadAssetBundle(bundleName,true);
                    return ab.LoadAsset<T>(assetName);
                }
            }
        }

        private ABData LoadAssetBundle(string bundleName,bool useNum)
        {
            float unLoadTime = GetUnLoadTime(bundleName);
            var assetbundle = AssetBundle.LoadFromFile(Path.Combine(rootPath, bundleName));
            ABData ab = new ABData(assetbundle, unLoadTime);
            abs.Add(bundleName, ab);
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
                    if (abs[bundlename].CanUnLoad())
                    {
                        //60秒后删除
                        abs[bundlename].unLoadTime = curTime + 60;
                    }
                    if (curTime >= abs[bundlename].unLoadTime)
                    {
                        PreUnloadBundle(bundlename);
                        abs[bundlename].UnLoad(false);
                        abs.Remove(bundlename);
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
                if (abs.ContainsKey(temp))
                {
                    abs[temp].UnUse();
                }
                else
                {
                    Debug.Log($"abs 中没有该ab包:{bundleName}");
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
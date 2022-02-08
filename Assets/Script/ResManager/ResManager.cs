using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Script.ResManager
{
    public class ResManager:MonoBehaviour
    {
        public static ResManager Inst;
        public Dictionary<string, List<Coroutine>> loadList;
        void Start()
        {
            Inst = this;
            loadList = new Dictionary<string, List<Coroutine>>();
        }
        private ResLoader loader;

        public void Init()
        {
            string path = null;
#if UNITY_EDITOR
            switch (GameMain.Inst.assetType)
            {
                case AssetLoadType.Editor:
                    loader = new Loder_Editor();
                    path = Application.dataPath;
                    break;
                case AssetLoadType.Bundle:
                    loader = new Loader_Bundle();
                    path = GameMain.Inst.assetRootPath;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
#else
            loader = new Loader_Bundle();
            path = Application.persistentDataPath;
#endif
            loader.Init(path);
        }

        public T Load<T>(string assetName) where T : Object
        {
            if (loader == null)
            {
                Init();
            }

            return loader.LoadAsset<T>(assetName);
        }

        public void LoadAsync<T>(string assetName, Action<T> callBack) where T : Object
        {
            if (loader == null)
            {
                Init();
            }

           var cor= StartCoroutine(loader.LoadAssetAsync<T>(assetName, delegate(T t)
           {
               if (loadList.ContainsKey(assetName))
               {
                   loadList.Remove(assetName);
               }
               callBack.Invoke(t);
           }));
           if (loadList.ContainsKey(assetName))
           {
               loadList[assetName].Add(cor);
           }
           else
           {
               List<Coroutine> l = new List<Coroutine>();
               l.Add(cor);
               loadList[assetName] = l;
           }
        }

        public void StopAllLoad()
        {
            if (loader is ResLoader_Stop)
            {
                ((ResLoader_Stop)loader).StopAllLoad();
            }
            StopAllCoroutines();
        }

        public void StopLoad(string assetName)
        {
            if (loader is ResLoader_Stop)
            {
                ((ResLoader_Stop)loader).StopLoad(assetName);
                if (loadList.ContainsKey(assetName))
                {
                    var list = loadList[assetName];
                    for (int i = 0; i < list.Count; i++)
                    {
                       StopCoroutine(list[i]);
                    }
                }
                loadList.Remove(assetName);
            }
        }
    }
}
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Script.ResManager
{
    public class ResManager:MonoBehaviour
    {
        public static ResManager Inst;

        void Start()
        {
            Inst = this;
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
            StartCoroutine(loader.LoadAssetAsync<T>(assetName,callBack));
        }

        public void StopAllLoad()
        {
            if (loader is ResLoader_Stop)
            {
                ((ResLoader_Stop)loader).StopAllLoad();
            }
            StopAllCoroutines();
        }
    }
}
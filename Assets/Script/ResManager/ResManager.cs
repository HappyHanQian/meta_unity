using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Script.ResManager
{
    public class ResManager
    {
        private static ResManager _inst;

        public static ResManager Inst
        {
            get
            {
                if (_inst == null)
                {
                    _inst = new ResManager();
                }

                return _inst;
            }
        }

        private ResManager()
        {
        }

        private ResLoader loader;

        public void Init()
        {
            string path = null;
#if UNITY_EDITOR
            switch (GameMain.Inst.assetType)
            {
                case AssetLoadType.Editor:
                    loader = new Res_Editor();
                    path = Application.dataPath;
                    break;
                case AssetLoadType.Bundle:
                    loader = new Res_Bundle();
                    path = GameMain.Inst.assetRootPath;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
#else
            loader = new Res_Bundle();
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

            return loader.Load<T>(assetName);
        }
    }
}
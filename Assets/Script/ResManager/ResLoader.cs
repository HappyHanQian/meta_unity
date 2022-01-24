using UnityEngine;

namespace Assets.Script.ResManager
{
    public interface ResLoader
    {
        void Init(string path);
        T Load<T>(string assetName) where T : Object;
    }
}
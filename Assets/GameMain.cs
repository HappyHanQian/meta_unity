using UnityEngine;

namespace Assets
{
    public class GameMain:MonoBehaviour
    {
        public static GameMain Inst;
        public AssetLoadType assetType;
        public string assetRootPath;

        void Awake()
        {
            Inst = this;
        }
    }

    public enum AssetLoadType
    {
        Editor,
        Bundle,
    }
}
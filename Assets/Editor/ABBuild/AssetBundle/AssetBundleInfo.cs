using ABBuild.Base;
using Boo.Lang;
using UnityEditor;

namespace ABBuild
{
    public class AssetBundleInfo
    {
        public string name;
        public List<Asset_Bundle> assets;

        public AssetBundleInfo(string name)
        {
            this.name = name;
            this.assets = new List<Asset_Bundle>();
        }

        public void RenameAssetBundle(string renameValue)
        {
            this.name = renameValue;
        }

        public void RemoveAsset(Asset_Bundle asset)
        {
            
        }

        public void AddAsset(Asset_Bundle asset)
        {
            assets.Add(asset);
        }
    }
}
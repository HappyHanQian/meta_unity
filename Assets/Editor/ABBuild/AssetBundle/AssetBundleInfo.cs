using System.Collections.Generic;
using UnityEditor;

    public class AssetBundleInfo
    {
        public string name;
        public string variant;
        public List<Asset_Bundle> assets;

        public AssetBundleInfo(string name,string variant)
        {
            this.name = name;
            this.variant = variant;
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

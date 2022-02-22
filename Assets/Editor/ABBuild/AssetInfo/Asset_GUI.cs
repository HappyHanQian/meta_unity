using System.Collections.Generic;
using System.Linq;

public class Asset_GUI : AssetBase
{
    /// <summary>
    /// 资源文件是否被勾选
    /// </summary>
    public bool isCheck;

    /// <summary>
    /// 文件夹是否展开
    /// </summary>
    public bool isExpanding;

    /// <summary>
    /// 文件夹的子资源(资源文件无效)
    /// </summary>
    public Dictionary<string, Asset_GUI> childAssetInfos_Dic;

    public List<Asset_GUI> childAssetInfos
    {
        get { return childAssetInfos_Dic.Values.ToList(); }
    }

    /// <summary>
    /// 构造资源文件类型
    /// </summary>
    /// <param name="fullPath"></param>
    /// <param name="name"></param>
    /// <param name="extension"></param>
    public Asset_GUI(string fullPath, string name, string extension) : base(fullPath, name, extension)
    {
        this.isCheck = false;
        this.isExpanding = false;
        this.childAssetInfos_Dic = null;
    }

    /// <summary>
    /// 构建文件夹资源
    /// </summary>
    /// <param name="fullPath"></param>
    /// <param name="name"></param>
    /// <param name="isExpanding"></param>
    public Asset_GUI(string fullPath, string name, bool isExpanding) : base(fullPath, name)
    {
        //构造界面信息
        this.isCheck = false;
        this.isExpanding = isExpanding;
        this.childAssetInfos_Dic = new Dictionary<string, Asset_GUI>();
    }

    public void AddChildAssetInfo_GUI(Asset_GUI assetGui)
    {
        if (childAssetInfos_Dic != null)
        {
            childAssetInfos_Dic.Add(assetGui.assetName, assetGui);
        }
    }

    public void RemoveChildAssetInfo_GUI(Asset_GUI assetGui)
    {
        if (childAssetInfos_Dic != null && childAssetInfos_Dic.ContainsKey(assetGui.assetName))
        {
            childAssetInfos_Dic.Remove(assetGui.assetName);
        }
    }

    public Asset_GUI GetChild_GUI(string assetName)
    {
        if (childAssetInfos_Dic == null)
        {
            return null;
        }

        if (childAssetInfos_Dic.ContainsKey(assetName))
        {
            return childAssetInfos_Dic[assetName];
        }

        return null;
    }
}

public enum FileType
{
    /// <summary>
    /// 有效文件资源
    /// </summary>
    ValidFile,

    /// <summary>
    /// 文件夹
    /// </summary>
    Folder,

    /// <summary>
    /// 无效文件资源
    /// </summary>
    InvalidFile,
}
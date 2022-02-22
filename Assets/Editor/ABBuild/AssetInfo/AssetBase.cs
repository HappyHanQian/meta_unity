using System;
using UnityEditor;
using UnityEngine;

public class AssetBase
{
    /// <summary>
    /// 资源完整路径
    /// </summary>
    public string assetFullPath;

    /// <summary>
    /// 资源路径
    /// </summary>
    public string assetPath;

    /// <summary>
    /// 资源名称
    /// </summary>
    public string assetName;

    /// <summary>
    /// GUID
    /// </summary>
    public string GUID;

    /// <summary>
    /// 资源文件类型
    /// </summary>
    public FileType assetFileType;

    /// <summary>
    /// 所属AB包(文件夹无效)
    /// </summary>
    public string bundled;

    /// <summary>
    /// 资源类型(文件夹无效 扩展名(脚本,图片,预制件...))
    /// </summary>
    public Type assetType;

    /// <summary>
    /// 构造资源文件类型
    /// </summary>
    /// <param name="fullPath"></param>
    /// <param name="name"></param>
    /// <param name="extension"></param>
    public AssetBase(string fullPath, string name, string extension)
    {
        this.assetFullPath = fullPath;
        this.assetPath = AssetTool.FullPath2AssetPath(fullPath);
        this.assetName = name;
        this.GUID = AssetDatabase.AssetPathToGUID(assetPath);
        this.assetFileType = AssetTool.GetFileTypeByExtension(extension);
        this.bundled = "";
        this.assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
    }

    /// <summary>
    /// 构建文件夹资源
    /// </summary>
    /// <param name="fullPath"></param>
    /// <param name="name"></param>
    public AssetBase(string fullPath, string name)
    {
        //我们需要这个文件夹的全路径
        this.assetFullPath = fullPath;
        //我们需要这个文件夹的Assets路径
        this.assetPath = "Assets" + fullPath.Replace(Application.dataPath.Replace("/", "\\"), "");
        //我们需要这个文件夹的名称
        this.assetName = name;
        //文件夹对象不需要GUID了
        this.GUID = "";
        //设置这是一个文件夹对象
        this.assetFileType = FileType.Folder;
        //文件夹不需要绑定AB包
        this.bundled = "";
        //文件夹对象没有具体的文件类型
        this.assetType = null;
    }
}
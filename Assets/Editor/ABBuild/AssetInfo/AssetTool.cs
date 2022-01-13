using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ABBuild
{
    public static class AssetTool
    {
        public static void ReadAssetsInChildren(Asset_GUI asset)
        {
            if (asset.assetFileType != FileType.Folder)
            {
                return;
            }
            DirectoryInfo dir = new DirectoryInfo(asset.assetFullPath);
            var files = dir.GetFileSystemInfos();
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                if (file is DirectoryInfo)
                {
                    //如果该内容是文件夹
                    //判断是否是无效的文件夹，比如是否是Editor，StreamingAssets等，这些文件夹中的东西是无法打进AB包的
                    if (IsValidFolder(file.Name))
                    {
                        //是有效的资源
                        //创建文件夹对象并加入到当前对象的子对象集合中
                        Asset_GUI ai = new Asset_GUI(file.FullName, file.Name, false);
                        asset.AddChildAssetInfo_GUI(ai);
                        //然后继续深层遍历这个文件夹
                        ReadAssetsInChildren(ai);
                    }
                    else
                    {
                        //无效的文件夹
                        //不用显示在界面上 也不用打进ab包
                    }
                }
                else
                {
                    //是文件
                    if (file.Extension != ".meta")
                    {
                        Asset_GUI ai = new Asset_GUI(file.FullName, file.Name, file.Extension);
                        asset.AddChildAssetInfo_GUI(ai);
                    }
                }
            }
        }

        /// <summary>
        /// 判断是否是一个有效的文件夹
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static bool IsValidFolder(string fileName)
        {
            return true;
        }
        /// <summary>
        /// 判断是否是一个有效的文件
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static FileType GetFileTypeByExtension(string extension)
        {
            switch (extension)
            {
                case ".cs":
                    return FileType.InvalidFile;
                default:
                    return FileType.ValidFile;
            }
        }

        /// <summary>
        /// 获取所有被选中的有效资源
        /// </summary>
        public static List<Asset_GUI> GetCheckedAssets(this List<Asset_GUI> validAssetList)
        {
            List<Asset_GUI> currentAssets = new List<Asset_GUI>();
            for (int i = 0; i < validAssetList.Count; i++)
            {
                if (validAssetList[i].isCheck)
                {
                    currentAssets.Add(validAssetList[i]);
                }
            }
            return currentAssets;
        }

        public static string AssetPath2FullPath(string assetPath)
        {
            var path = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
            return path.Replace("/", "\\");
        }

        public static string FullPath2AssetPath(string fullPath)
        {
            int index = fullPath.IndexOf("Assets");
            if (index>=0)
            {
                string assetpath = fullPath.Substring(index);
                return assetpath.Replace("\\","/");
            }

            return "";
        }
    }
}
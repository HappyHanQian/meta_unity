using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ABBuild
{
    public class AB_Build_Window : EditorWindow
    {
        private Assets_GUI _assets;
        private AssetBundleInfos _assetBundle;

        [MenuItem("MyTools/AB_Build_Window")]
        static void OpenWindow()
        {
            var window = GetWindow<AB_Build_Window>("AssetBundle");
            window.minSize = new Vector2(685.0f, 567.0f);
            
            window.Show();
            window._buildPath = Path.Combine(Path.GetDirectoryName(Application.dataPath),"Build", "AssetsBunde");
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptReloaded()
        {
            
        }
        private void InitAsset()
        {
            _assets = new Assets_GUI();
        }

        private void InitAssetBundle()
        {
            if (_assetBundle == null)
            {
                _assetBundle = new AssetBundleInfos();
            }
            else
            {
                _assetBundle.Clear();
            }
        }

        private GUIStyle s = new GUIStyle();

        void OnGUI()
        {
            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.Space(20);
                s.fontSize = 40;
                s.alignment = TextAnchor.MiddleCenter;
                s.normal.textColor = Color.white;
                GUILayout.Label("Compiling...", s, GUILayout.Height(position.height));
            }
            else
            {
                TitleGUI();
                AssetBundleGUI();
                CurrentAssetBundleGUI();
                AssetGUI();
            }
        }

        #region 标题栏

        //标记，用于标记当前选中的AB包索引
        private string _currentAB = string.Empty;
        private string _currentVar = string.Empty;

        //是否隐藏无效资源
        private bool _hideInvalidAsset = false;

        //是否隐藏已绑定资源
        private bool _hideBundleAsset = false;

        //打包路径
        private string _buildPath = "";

        //打包平台
        private BuildTarget _buildTarget = BuildTarget.StandaloneWindows;
        private List<Asset_GUI> _validAssets;

        private void TitleGUI()
        {
            if (GUI.Button(new Rect(5, 5, 60, 15), "Create", "PreButton"))
            {
                Creat();
            }

            //当前未选中任一AB包的话，禁用之后的所有UI控件
            GUI.enabled = _currentAB == string.Empty ? false : true;
            if (GUI.Button(new Rect(65, 5, 60, 15), "Rename", "PreButton"))
            {
                _isRename = true;
            }

            if (GUI.Button(new Rect(125, 5, 60, 15), "Clear", "PreButton"))
            {
                if (EditorUtility.DisplayDialog("Prompt", "Clear " + _assetBundle.bundlesDic[_currentAB][_currentVar].name + " ？",
                    "Yes", "No"))
                {
                    _assetBundle.bundlesDic[_currentAB][_currentVar].ClearAsset();
                }
            }

            if (GUI.Button(new Rect(185, 5, 60, 15), "Delete", "PreButton"))
            {
                if (EditorUtility.DisplayDialog("Prompt",
                    "Delete " + _assetBundle.bundlesDic[_currentAB][_currentVar].name + "？This will clear all assets！",
                    "Yes", "No"))
                {
                    _assetBundle.DeleteAssetBundle(_currentAB,_currentVar);
                    _currentAB = string.Empty;
                    _currentVar = string.Empty;
                }
            }

            if (GUI.Button(new Rect(250, 5, 100, 15), "Add Assets", "PreButton"))
            {
                if (_validAssets==null)
                {
                    return;
                }
                List<Asset_GUI> assets = _validAssets.GetCheckedAssets();
                for (int i = 0; i < assets.Count; i++)
                {
                    // var bundle_asset = _assetBundle.GetBundleAsset(assets[i].assetPath);
                    // _assetBundle.assetBundles[_currentAB].AddAsset(bundle_asset);
                }

                ClearValidList();
            }

            //取消UI控件的禁用
            GUI.enabled = true;

            _hideInvalidAsset = GUI.Toggle(new Rect(360, 5, 100, 15), _hideInvalidAsset, "Hide Invalid");
            _hideBundleAsset = GUI.Toggle(new Rect(460, 5, 100, 15), _hideBundleAsset, "Hide Bundled");

            if (GUI.Button(new Rect(250, 25, 60, 15), "Open", "PreButton"))
            {
                if (!string.IsNullOrEmpty(_buildPath))
                {
                    EditorUtility.OpenFilePanel("AB包", _buildPath, "");
                }
            }

            if (GUI.Button(new Rect(310, 25, 60, 15), "Browse", "PreButton"))
            {
                _buildPath = EditorUtility.OpenFolderPanel("选择打包路径", _buildPath, "");
            }

            GUI.Label(new Rect(370, 25, 70, 15), "Build Path:");
            _buildPath = GUI.TextField(new Rect(440, 25, 300, 15), _buildPath);
            GUI.Label(new Rect((int) position.width - 245, 5, 40, 15), "平台:", "PreLabel");
            BuildTarget buildTarget =
                (BuildTarget) EditorGUI.EnumPopup(new Rect((int) position.width - 205, 5, 150, 15), _buildTarget,
                    "PreDropDown");
            if (GUI.Button(new Rect((int) position.width - 55, 5, 50, 15), "Build", "PreButton"))
            {
                if (string.IsNullOrEmpty(_buildPath))
                {
                    EditorUtility.DisplayDialog("提示", "ab包输出路径不能为空", "OK");
                    return;
                }

                if (_assetBundle==null)
                {
                    Creat();
                }
                var option = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.DeterministicAssetBundle;
                AssetBundleTool.BuildAssetBundles(_assetBundle,_buildPath, option, BuildTarget.StandaloneWindows64);
                EditorUtility.DisplayDialog("提示", "打包完成", "OK");
            }

            GUIContent content = EditorGUIUtility.IconContent("d_RotateTool");
            if (GUI.Button(new Rect((int) (position.width - 25), 25, 20, 20), content, "PreButton"))
            {
                InitAsset();
            }
        }

        private void Creat()
        {
            InitAssetBundle();
            _assetBundle.Creat();
            _assets.SetBundled(_assetBundle.bundlesDic);
        }

        private void ClearValidList()
        {
            if (_validAssets==null)
            {
                return;
            }
            for (int i = 0; i < _validAssets.Count; i++)
            {
                _validAssets[i].isCheck = false;
            }
            _validAssets.Clear();
        }

        #endregion

        #region AssetBundle分区

        //区域视图的范围
        private Rect _ABViewRect;

        //区域视图滚动的范围
        private Rect _ABScrollRect;

        //区域视图滚动的位置
        private Vector2 _ABScroll;

        private int _ABViewWidth;
        //区域高度标记,用来控制视图滚动量的
        private int _ABViewHeight = 0;
        /// <summary>
        /// 是否在重命名
        /// </summary>
        private bool _isRename;
        /// <summary>
        /// 重命名
        /// </summary>
        private string _renameValue;
        private void AssetBundleGUI()
        {
            //区域的视图范围：左上角位置固定，宽度固定（240），高度为窗口高度的一半再减去标题栏高度（20），标题栏高度为什么是20？看一下标题栏的控件高度就行了呗，多余的是空隙之类的
            _ABViewRect = new Rect(5, 25, 240, (int) position.height / 2 - 20);
            //滚动的区域是根据当前显示的控件数量来确定的，如果显示的控件（AB包）太少，则滚动区域小于视图范围，则不生效，_ABViewHeight会根据AB包数量累加
            _ABScrollRect = new Rect(5, 25, _ABViewWidth, _ABViewHeight);
            _ABScroll = GUI.BeginScrollView(_ABViewRect, _ABScroll, _ABScrollRect);
            GUI.BeginGroup(_ABScrollRect, "", "Box");
            _ABViewHeight = 0;
            _ABViewWidth = 0;
            if (_assetBundle != null)
            {
                BundleGUI();
            }
            else
            {
                _currentAB = string.Empty;
            }

            //Begin和End中间就是我们要显示的控件列表，当然，如果AB包数量太少，我们的滚动区域还是不能小于视图区域
            if (_ABViewHeight < _ABViewRect.height)
            {
                _ABViewHeight = (int) _ABViewRect.height;
            }

            if (_ABViewWidth< _ABViewRect.width)
            {
                _ABViewWidth = (int)_ABViewRect.width;
            }

            GUI.EndGroup();
            GUI.EndScrollView();
        }

        private void BundleGUI()
        {
            _ABViewHeight = 5;
            foreach (var bundle_name in _assetBundle.bundlesDic.Keys)
            {
                int length = bundle_name.Length;
                int width = length * 6 + 30;
                if (_ABViewWidth < width)
                {
                    _ABViewWidth = width;
                }
            }
            
            foreach (var bundle_name in _assetBundle.bundlesDic.Keys)
            {
                foreach (var bundle in _assetBundle.bundlesDic[bundle_name].Values)
                {
                    string icon = bundle.assets.Count > 0 ? "Prefab Icon" : "d_Prefab On Icon";
                    if (bundle_name == _currentAB&&bundle.variant==_currentVar)
                    {
                        //选中的bundle
                        GUI.Box(new Rect(0, _ABViewHeight, _ABViewWidth, 15), "", "OL SelectedRow");
                        if (_isRename)
                        {
                            //重命名
                            GUIContent content = EditorGUIUtility.IconContent(icon);
                            content.text = "";
                            GUI.Label(new Rect(5, _ABViewHeight, _ABViewWidth - 10, 15), content, "PrebLabel");
                            _renameValue = GUI.TextField(new Rect(40, _ABViewHeight, 140, 15), _renameValue);
                            //重命名OK
                            if (GUI.Button(new Rect(180, _ABViewHeight, 30, 15), "OK", "minibuttonleft"))
                            {
                                if (!string.IsNullOrEmpty(_renameValue))
                                {
                                    if (!_assetBundle.IsExistName(_renameValue))
                                    {
                                        bundle.RenameAssetBundle(_renameValue);
                                        _renameValue = "";
                                        _isRename = false;
                                    }
                                    else
                                    {
                                        EditorUtility.DisplayDialog("提示", $"Already existed name:{_renameValue}", "OK");
                                    }
                                }
                            }

                            //重命名NO
                            if (GUI.Button(new Rect(210, _ABViewHeight, 30, 15), "NO", "minibuttonleft"))
                            {
                                _isRename = false;
                            }
                        }
                        else
                        {
                            GUIContent content = EditorGUIUtility.IconContent(icon);
                            content.text = bundle_name;
                            GUI.Label(new Rect(5, _ABViewHeight, _ABViewWidth-10, 15), content, "PR PrefabLabel");
                        }
                    }
                    else
                    {
                        GUIContent content = EditorGUIUtility.IconContent(icon);
                        content.text = bundle.name;
                        if (GUI.Button(new Rect(5, _ABViewHeight, _ABViewWidth-10, 15), content, "PR PrefabLabel"))
                        {
                            _currentAB = bundle_name;
                            _currentVar = bundle.variant;
                            _currentABAsset = -1;
                            _isRename = false;
                        }
                    }

                    _ABViewHeight += 20;

                }
            }
            _ABViewHeight += 5;
        }

        #endregion

        #region AB预览
        //区域视图的范围
        private Rect _currentABViewRect;
        //区域视图滚动的范围
        private Rect _currentABScrollRect;
        //区域视图滚动的位置
        private Vector2 _currentABScroll;
        //区域高度标记
        private int _currentABViewHeight = 0;
        //区域宽度标记
        private int _currentABViewWidth = 0;
        /// <summary>
        /// 当前选中的资源索引
        /// </summary>
        private int _currentABAsset = -1;
        private void CurrentAssetBundleGUI()
        {
            //区域的视图范围：左上角位置固定在上一个区域的底部，宽度固定（240），高度为窗口高度的一半再减去空隙（15），上下都有空隙
            _currentABViewRect = new Rect(5, (int) position.height / 2 + 10, 240, (int) position.height / 2 - 15);
            _currentABScrollRect = new Rect(5, (int) position.height / 2 + 10, _currentABViewWidth, _currentABViewHeight);
            _currentABScroll = GUI.BeginScrollView(_currentABViewRect, _currentABScroll, _currentABScrollRect);
            GUI.BeginGroup(_currentABScrollRect, "", "Box");
            _currentABViewHeight = 0;
            _currentABViewWidth = 0;
            if (_assetBundle != null)
            {
                BundleAssetGUI();
            }
            else
            {
                _currentABAsset = -1;
            }
            if (_currentABViewHeight < _currentABViewRect.height)
            {
                _currentABViewHeight = (int) _currentABViewRect.height;
            }

            if (_currentABViewWidth<_currentABViewRect.width)
            {
                _currentABViewWidth=(int)_currentABViewRect.width;
            }

            GUI.EndGroup();
            GUI.EndScrollView();
        }

        private void BundleAssetGUI()
        {
            _currentABViewHeight = 5;
            if (_assetBundle.bundlesDic.ContainsKey(_currentAB)&&_assetBundle.bundlesDic[_currentAB].ContainsKey(_currentVar))
            {
                AssetBundleInfo bundle = _assetBundle.bundlesDic[_currentAB][_currentVar];
                for (int i = 0; i < bundle.assets.Count; i++)
                {
                    var asset = bundle.assets[i];
                    int width = asset.assetName.Length * 6 + 30;
                    if (_currentABViewWidth < width)
                    {
                        _currentABViewWidth = width;
                    }
                }
                for (int i = 0; i < bundle.assets.Count; i++)
                {
                    var asset = bundle.assets[i];
                    if (i == _currentABAsset)
                    {
                        GUI.Box(new Rect(0, _currentABViewHeight, _currentABViewWidth, 15), "", "OL SelectedRow");
                    }

                    GUIContent content = EditorGUIUtility.ObjectContent(null, asset.assetType);
                    content.text = asset.assetName;
                    if (GUI.Button(new Rect(0, _currentABViewHeight, _currentABViewWidth, 15), content, "PR PrefabLabel"))
                    {
                        _currentABAsset = i;
                    }

                    //在Button控件右方绘制减号Button控件，当点击时，删除此资源对象在当前选中的AB包中
                    if (GUI.Button(new Rect(215, _currentABViewHeight, 20, 15), "", "OL Minus"))
                    {
                        bundle.RemoveAsset(asset);
                        _currentABAsset = -1;
                    }

                    _currentABViewHeight += 20;
                }
            }

            _currentABViewHeight += 5;
        }

        #endregion

        #region Asset目录

        //区域视图的范围
        private Rect _assetViewRect;

        //区域视图滚动的范围
        private Rect _assetScrollRect;

        //区域视图滚动的位置
        private Vector2 _assetScroll;

        //区域高度标记，这里不用管它，是后续用来控制视图滚动量的
        private int _assetViewHeight = 0;

        private void AssetGUI()
        {
            //区域的视图范围：左上角位置固定，宽度为窗口宽度减去左边的区域宽度以及一些空隙（255），高度为窗口高度减去上方两层标题栏以及一些空隙（50）
            _assetViewRect = new Rect(250, 45, (int) position.width - 255, (int) position.height - 50);
            _assetScrollRect = new Rect(250, 45, (int) position.width - 255, _assetViewHeight);
            _assetScroll = GUI.BeginScrollView(_assetViewRect, _assetScroll, _assetScrollRect);
            GUI.BeginGroup(_assetScrollRect, "", "Box");
            _assetViewHeight = 0;
            if (_assets == null)
            {
                InitAsset();
            }
            AssetGUI(_assets.rootAsset, 0);
            if (_assetViewHeight < _assetViewRect.height)
            {
                _assetViewHeight = (int) _assetViewRect.height;
            }

            GUI.EndGroup();
            GUI.EndScrollView();
        }

        /// <summary>
        /// 展示一个资源对象的GUI，indentation为缩进等级，子对象总比父对象大
        /// </summary>
        private void AssetGUI(Asset_GUI asset, int indentation)
        {
            //开启一行
            GUILayout.BeginHorizontal();
            //以空格缩进
            GUILayout.Space(indentation * 20 + 5);
            if (asset.assetFileType == FileType.Folder)
            {
                //如果是文件夹
                AssetFolderGUI(asset);
            }
            else
            {
                //是文件
                AssetFileGUI(asset);
            }

            //结束一行
            //每一行的高度20，让高度累加
            _assetViewHeight += 20;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            //如果当前文件夹是展开的，换一行进行深层遍历其子对象，且缩进等级加1
            if (asset.isExpanding)
            {
                for (int i = 0; i < asset.childAssetInfos.Count; i++)
                {
                    AssetGUI(asset.childAssetInfos[i], indentation + 1);
                }
            }
        }

        private void AssetFileGUI(Asset_GUI asset)
        {
            if (_hideInvalidAsset && asset.assetFileType == FileType.InvalidFile)
            {
                return;
            }

            if (_hideBundleAsset && !string.IsNullOrEmpty(asset.bundled))
            {
                return;
            }

            GUI.enabled = !(asset.assetFileType == FileType.InvalidFile || asset.bundled != "");
            // EditorGUI.BeginDisabledGroup((asset.assetFileType == FileType.InvalidFile || asset.bundled != ""));
            if (GUILayout.Toggle(asset.isCheck, "", GUILayout.Width(20)) != asset.isCheck)
            {
                asset.isCheck = !asset.isCheck;
                AddOrRemove2ValidList(asset);
            }

            GUILayout.Space(10);
            GUIContent content = EditorGUIUtility.ObjectContent(null, asset.assetType);
            content.text = asset.assetName;
            GUILayout.Label(content, GUILayout.Height(20));
            GUI.enabled = true;
            // EditorGUI.EndDisabledGroup();
            if (asset.bundled != "")
            {
                GUILayout.Label($"[{asset.bundled}]", "PR PrefabLabel");
            }
        }

        private void AssetFolderGUI(Asset_GUI asset)
        {
            if (GUILayout.Toggle(asset.isCheck, "", GUILayout.Width(20)) != asset.isCheck)
            {
                asset.isCheck = !asset.isCheck;
            }
            string icon = asset.isExpanding ? "d_OpenedFolder Icon" : "Folder Icon";
            GUIContent content = EditorGUIUtility.IconContent(icon);
            content.text = asset.assetName;
            asset.isExpanding = EditorGUILayout.Foldout(asset.isExpanding, content);
        }

        private void AddOrRemove2ValidList(Asset_GUI asset)
        {
            if (asset.assetFileType==FileType.ValidFile)
            {
                if (asset.isCheck)
                {
                    if (_validAssets == null)
                    {
                        _validAssets = new List<Asset_GUI>();
                    }
                    _validAssets.Add(asset);
                }
                else
                {
                    if (_validAssets!=null&&_validAssets.Contains(asset))
                    {
                        _validAssets.Remove(asset);
                    }
                }

            }
        }
        #endregion
    }
}
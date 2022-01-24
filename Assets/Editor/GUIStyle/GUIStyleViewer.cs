using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GUIStyleViewer : EditorWindow
{
    private Vector2 scrollVector2 = Vector2.zero;
    private string search = "";
    bool _showingStyles;
    private bool _showingIcons;
    private Texture2D[] _objects;

    [MenuItem("MyTools/GUIStyle查看器")]
    public static void InitWindow()
    {
        GUIStyleViewer window =  (GUIStyleViewer)EditorWindow.GetWindow(typeof(GUIStyleViewer));
        window._showingStyles = true;
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(_showingStyles, "Styles", EditorStyles.toolbarButton) != _showingStyles)
        {
            _showingStyles = !_showingStyles;
            _showingIcons = !_showingStyles;
        }

        if (GUILayout.Toggle(_showingIcons, "Icons", EditorStyles.toolbarButton) != _showingIcons)
        {
            _showingIcons = !_showingIcons;
            _showingStyles = !_showingIcons;
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.BeginHorizontal("HelpBox");
        GUILayout.Space(30);
        search = EditorGUILayout.TextField("", search, "SearchTextField", GUILayout.MaxWidth(position.x / 3));
        GUILayout.Label("", "SearchCancelButtonEmpty");
        GUILayout.EndHorizontal();
        if (_showingStyles)
        {
            StylesGUI();
        }
        else if (_showingIcons)
        {
            IconGUI();
        }
    }

    private void StylesGUI()
    {
        scrollVector2 = GUILayout.BeginScrollView(scrollVector2);
        foreach (GUIStyle style in GUI.skin.customStyles)
        {
            if (style.name.ToLower().Contains(search.ToLower()))
            {
                DrawStyleItem(style);
            }
        }

        GUILayout.EndScrollView();
    }

    void DrawStyleItem(GUIStyle style)
    {
        GUILayout.BeginHorizontal("box");
        GUILayout.Space(40);
        EditorGUILayout.SelectableLabel(style.name);
        GUILayout.FlexibleSpace();
        EditorGUILayout.SelectableLabel(style.name, style);
        GUILayout.Space(40);
        EditorGUILayout.SelectableLabel("", style, GUILayout.Height(40), GUILayout.Width(40));
        GUILayout.Space(50);
        if (GUILayout.Button("复制到剪贴板"))
        {
            TextEditor textEditor = new TextEditor();
            textEditor.text = style.name;
            textEditor.OnFocus();
            textEditor.Copy();
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    private void IconGUI()
    {
        if (_objects == null)
        {
            _objects = Resources.FindObjectsOfTypeAll<Texture2D>();
        }

        scrollVector2 = GUILayout.BeginScrollView(scrollVector2);
        for (int i = 0; i < _objects.Length; i++)

        {
            Texture texture = _objects[i];
            if (!string.IsNullOrEmpty(texture.name) && texture.name.ToLower().Contains(search))
            {
                DrawIconItem(texture);
            }
        }

        GUILayout.EndScrollView();
    }

    private void DrawIconItem(Texture texture)
    {
        GUILayout.BeginHorizontal("box");
        GUILayout.Space(40);
        EditorGUILayout.SelectableLabel(texture.name);
        GUILayout.FlexibleSpace();
        Rect textureRect = GUILayoutUtility.GetRect(texture.width, texture.width, texture.height, texture.height,
            GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
        EditorGUI.DrawTextureTransparent(textureRect, texture);
        GUILayout.Space(50);
        if (GUILayout.Button("复制到剪贴板"))
        {
            TextEditor textEditor = new TextEditor();
            textEditor.text = texture.name;
            textEditor.OnFocus();
            textEditor.Copy();
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }
}
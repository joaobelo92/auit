using System.Runtime.Remoting.Contexts;
using UnityEditor;
using UnityEngine;

public class GalleryView : MonoBehaviour
{
    public static int SELECTED_WIDTH = 256, SELECTED_HEIGHT = 144;

    private Texture2D m_selectedView; 
    public Texture2D SelectedView {  
        get { return m_selectedView; } 
    }

    private string m_selectedInfo;
    public string SelectedInfo
    {
        get { return m_selectedInfo; }
    } 
   
    public void SetSelected(Texture2D view, string info)
    {
        m_selectedView = view;
        m_selectedInfo = info;
    }
}

[CustomEditor(typeof(GalleryView))]
public class GalleryViewEditor : Editor
{
    private Vector2 selectedInfoScrollPosition;

    public override void OnInspectorGUI()
    {
        GalleryView galleryView = (GalleryView)target;

        EditorGUILayout.LabelField("Selected", EditorStyles.boldLabel);

        // Selected 
        EditorGUILayout.BeginHorizontal();
        // View
        if (galleryView.SelectedView != null)
        {
            GUILayout.Box(galleryView.SelectedView, GUILayout.Width(GalleryView.SELECTED_WIDTH), GUILayout.Height(GalleryView.SELECTED_HEIGHT));
        } else
        {
            GUILayout.Box("", GUILayout.Width(GalleryView.SELECTED_WIDTH), GUILayout.Height(GalleryView.SELECTED_HEIGHT));
        }
        // Info
        EditorGUILayout.BeginVertical(GUILayout.Height(GalleryView.SELECTED_HEIGHT), GUILayout.ExpandWidth(true));
        selectedInfoScrollPosition = EditorGUILayout.BeginScrollView(selectedInfoScrollPosition,
           GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

        GUILayout.Label(galleryView.SelectedInfo, EditorStyles.wordWrappedLabel, GUILayout.ExpandWidth(true));


        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }
}

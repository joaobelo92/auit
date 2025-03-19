using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GalleryView : MonoBehaviour
{
    public delegate void OnSaveSelected(); 
    public OnSaveSelected onSaveSelected;

    public delegate void OnClearSelected();
    public OnClearSelected onClearSelected;

    public delegate void OnClearSaved();
    public OnClearSaved onClearSaved;

    public delegate void OnHoverSelected(bool hover);
    public OnHoverSelected onHoverSelected;

    public static int SELECTED_WIDTH = 256, SELECTED_HEIGHT = 144;
    public static int SAVED_WIDTH = 192, SAVED_HEIGHT = 108;

    private Texture2D m_selectedView; 
    public Texture2D SelectedView {  
        get { return m_selectedView; } 
    }

    private string m_selectedInfo;
    public string SelectedInfo
    {
        get { return m_selectedInfo; }
    }

    private List<Texture2D> m_savedViews = new List<Texture2D>();
    public List<Texture2D> SavedViews
    {
        get { return m_savedViews; }
    }

    public void ResetSelected()
    {
        m_selectedView = null;
        m_selectedInfo = string.Empty;
    }
   
    public void SetSelected(Texture2D view, string info)
    {
        m_selectedView = view;
        m_selectedInfo = info;
    }


    public void ClearSelected()
    {
        if (onClearSelected != null)
        {
            onClearSelected();
        }
    }


    public void SaveSelected()
    {
        if (onSaveSelected != null)
        {
            onSaveSelected();
        }
    }

    public void SetSaved(List<Texture2D> savedViews)
    {
        m_savedViews = savedViews;
    }

    public void ClearSaved()
    {
        if (onClearSaved != null)
        {
            onClearSaved();
        }
    }

    public void HoverSelected(bool hover)
    {
        if (onHoverSelected != null)
        {
            onHoverSelected(hover);
        }
    }

}

[CustomEditor(typeof(GalleryView))]
public class GalleryViewEditor : Editor
{
    private Vector2 selectedInfoScrollPosition;
    private Vector2 savedScrollPosition;
    private bool hoverSelected;

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
            
            // Hover over selected
            Rect crSelected = GUILayoutUtility.GetLastRect();
            Event e = Event.current; 
            bool hoverSelectedCurrent = crSelected.Contains(Event.current.mousePosition);
            if (hoverSelectedCurrent != hoverSelected)
            {
                hoverSelected = hoverSelectedCurrent;
                galleryView.HoverSelected(hoverSelected);
            }
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

        if (galleryView.SelectedView != null)
        {
            if (GUILayout.Button("Save Selected"))
            {
                galleryView.SaveSelected();
            }
            if (GUILayout.Button("Clear Selected"))
            {
                galleryView.ClearSelected();
            }
        }
        EditorGUILayout.Space(20);


        EditorGUILayout.LabelField("Saved", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear Saved"))
        {
            galleryView.ClearSaved();
        }
        int numSaved = galleryView.SavedViews.Count;
        EditorGUILayout.BeginVertical(GUILayout.Height(2 * GalleryView.SAVED_HEIGHT), GUILayout.ExpandWidth(true));
        savedScrollPosition = EditorGUILayout.BeginScrollView(savedScrollPosition,
           GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

        float savedViewWidth = EditorGUIUtility.currentViewWidth;
        int numSavedPerRow = (int)((savedViewWidth - 50) / (GalleryView.SAVED_WIDTH + 10));
        if (numSavedPerRow <= 0)
        {
            numSavedPerRow = 1;
        }
        for (int i = 0; i < numSaved; i += numSavedPerRow)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < numSavedPerRow && i + j < numSaved; j++)
            {
                GUILayout.Box(galleryView.SavedViews[i + j], GUILayout.Width(GalleryView.SAVED_WIDTH), GUILayout.Height(GalleryView.SAVED_HEIGHT));
                GUILayout.Space(10);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();


    }
}


using UnityEditor;
using UnityEngine;


public class SingleAttributeControllers : MonoBehaviour
{
    public float m_chartHeight = 50;
    public Color m_chartColor = new Color(0.2f, 0.2f, 0.2f);
}

[CustomEditor(typeof(SingleAttributeControllers))]
public class SingleAttributeControllersEditor : Editor
{
    SingleAttributeControllers sacs;
    SingleAttributeController sac1;
    SingleAttributeController sac2;

    /*
    [MenuItem("AdaptLens/Single Attribute Controllers")]
    public static void ShowWindow()
    {
        GetWindow<SingleAttributeControllers>("Single Attribute Controllers");
    }
    */

    public void OnEnable()
    {
        sac1 = new SingleAttributeController("Field of View Weight");
        for (int i = 0; i < 20; i++)
        {
            sac1.AddValue(Random.Range(0f,1f));
        }
        sac2 = new SingleAttributeController("Distance Interval Weight");
        for (int i = 0; i < 20; i++)
        {
            sac2.AddValue(Random.Range(0f, 1f));
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        sacs = (SingleAttributeControllers)target;

        
        sac1.Draw();
        EditorGUILayout.Space();

        
        sac2.Draw();
    }
}

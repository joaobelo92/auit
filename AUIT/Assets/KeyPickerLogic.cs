using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class KeyPickerLogic : MonoBehaviour
{
    public List<GameObject> taskKeys = new List<GameObject>();
    public GameObject keyChainTarget;



    // Start is called once before the first execution of Update after the MonSoBehaviour is created
    void Start()
    {
        // for (int i = 0; i < transform.childCount; i++)
        // {
        //     taskKeys.Add(transform.GetChild(i).gameObject);
        // }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetupTask()
    {
        SetupTask(new (int, int)[] { (0, 0), (1, 1), (2, 2), (3, 3), (4, 4) }, 3);
    }

    public void SetupTask((int, int)[] taskKeyIndices, int targetKeyIndex)
    {
        for (int i = 0; i < taskKeys.Count; i++)
        {
            if (i < taskKeyIndices.Length)
            {
                taskKeys[i].GetComponent<KeyLogic>().SetupKey(taskKeyIndices[i]);
            }
            else
            {
                throw new System.Exception("Not enough task keys provided for the task key indices.");
            }
        }

        
        keyChainTarget.GetComponent<KeyLogic>().SetupKey(taskKeyIndices[targetKeyIndex]);
    }


}

[CustomEditor(typeof(KeyPickerLogic))]
public class KeyPickerLogicEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        KeyPickerLogic keyPickerLogic = (KeyPickerLogic)target;
        if (GUILayout.Button("Setup Task"))
        {
            keyPickerLogic.SetupTask();
        }
    }
}

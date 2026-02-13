using System.Collections.Generic;
using Cysharp.Threading.Tasks.Triggers;
using Oculus.Interaction.HandGrab;
using UnityEngine;

public class KeyLogic : MonoBehaviour
{
    private List<GameObject> handles = new List<GameObject>();
    private List<GameObject> ends = new List<GameObject>();

    public GameObject bow;
    public GameObject tip;

    public BoxCollider boxCollider0;
    public BoxCollider boxCollider1;

    private Vector3 startPos;
    private Quaternion startRot;

    
    public GameObject keyBox;
    public GameObject keyPicker;

    private StudyControlPanel studyControlPanel;

    void Start()
    {
        Transform handlesGO = transform.Find("KeyHandle");
        for (int i = 0; i < handlesGO.childCount; i++)
        {
            handles.Add(handlesGO.GetChild(i).gameObject);
        }
        Transform endsGO = transform.Find("KeyEnd");
        for (int i = 0; i < endsGO.childCount; i++)
        {
            ends.Add(endsGO.GetChild(i).gameObject);
        }

        startPos = transform.position;
        startRot = transform.rotation;

        studyControlPanel = FindFirstObjectByType<StudyControlPanel>();

        InvokeRepeating(nameof(CheckIfAtDropBox), 0f, 0.2f);
    }

    private void CheckIfAtDropBox()
    {
        if (boxCollider0 != null && boxCollider0.bounds.Contains(transform.position))
        {
            Debug.Log("Key is within bounds of the box collider 0.");
            studyControlPanel.AdvanceTask(gameObject.transform.parent.gameObject, 0);
        }
        if (boxCollider1 != null && boxCollider1.bounds.Contains(transform.position))
        {
            Debug.Log("Key is within bounds of the box collider 1.");
            studyControlPanel.AdvanceTask(gameObject.transform.parent.gameObject, 1);
        }
    }


    public void SetupKey((int, int) keyIndex)
    {

        for (int i = 0; i < handles.Count; i++)
        {
            handles[i].SetActive(keyIndex.Item1 == i);
        }

        for (int i = 0; i < ends.Count; i++)
        {
            ends[i].SetActive(keyIndex.Item2 == i);
        }

    }

    public void toggleBowTip()
    {
        keyBox.SetActive(false);
        keyPicker.SetActive(true);
        bow.SetActive(!bow.activeSelf);
        tip.SetActive(!tip.activeSelf);
    }

    public void ResetKey()
    {
        transform.position = startPos;
        transform.rotation = startRot;
    }



}

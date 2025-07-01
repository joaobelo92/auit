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

    public bool meshRequired = true; 

    void Start()
    {
        if (meshRequired)
            CombineMesh();
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

        print($"KeyLogic Start: {handles.Count} handles, {ends.Count} ends");
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

        if (meshRequired)
            CombineMesh();

    }

    public void toggleBowTip()
    {
        bow.SetActive(!bow.activeSelf);
        tip.SetActive(!tip.activeSelf);
    }

    private void CombineMesh()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = combinedMesh;
    }

}

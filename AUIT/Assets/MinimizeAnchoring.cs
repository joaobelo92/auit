using System;
using System.Collections;
using UnityEngine;

public class MinimizeAnchoring : MonoBehaviour
{
    public GameObject anchoringControls;

    public GameObject[] anchoringIcons;


    public void ShowAnchoringControls()
    {
        anchoringControls.SetActive(true);
    }

    public void HideAnchoringControls()
    {

        // anchoringControls.SetActive(false);
        StartCoroutine(HideControlsAfterDelay());
    }
    
    public void UpdateAnchoringIcon(int show)
    {
        for (int i = 0; i < anchoringIcons.Length; i++)
        {
            anchoringIcons[i].SetActive(i == show);
        }
    }

    private IEnumerator HideControlsAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        anchoringControls.SetActive(false);
    }
}

using AUIT.AdaptationObjectives.Definitions;
using AUIT.PropertyTransitions;
using UnityEngine;

public class ReferenceFrameControls : MonoBehaviour
{
    public CoordinateSystemTransition transitionProperty;

    // public GameObject buttonUserReferenceFrame;
    // public GameObject buttonWorldReferenceFrame;

    // public GameObject userReferenceFrame;

    public void SelectTorsoAsReferenceFrame()
    {
        // if (userReferenceFrame != null)
        // {
        //     transform.parent = userReferenceFrame.transform;
        // }
        // else
        // {
        //     Debug.LogError("User Reference Frame is not assigned.");
        // }
        transitionProperty.Adapt(new Layout(transform.position, transform.rotation, CoordinateSystem.Torso));
    }

    public void SelectHeadAsReferenceFrame()
    {
        transitionProperty.Adapt(new Layout(transform.position, transform.rotation, CoordinateSystem.Head));
    }

    public void SelectWorldAsReferenceFrame()
    {
        transitionProperty.Adapt(new Layout(transform.position, transform.rotation, CoordinateSystem.World));
    }


}

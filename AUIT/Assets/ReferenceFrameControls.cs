using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Objectives;
using AUIT.PropertyTransitions;
using UnityEngine;

public class ReferenceFrameControls : MonoBehaviour
{
    public CoordinateSystemTransition transitionProperty;
    public StudyControlPanel studyControlPanel;

    public UpdateCoordinateSystemOnMovement updateObjective;

    // public GameObject buttonUserReferenceFrame;
    // public GameObject buttonWorldReferenceFrame;

    // public GameObject userReferenceFrame;

    void Start()
    {
    }

    public void SelectTorsoAsReferenceFrame()
    {
        transitionProperty.Adapt(new Layout(transform.position, transform.rotation, CoordinateSystem.Torso));
        if (studyControlPanel.adaptationType == AdaptationType.Adaptive)
        {
            updateObjective.coordinateSystemWhileMoving = CoordinateSystem.Torso;
        }
    }
    
    public void SelectLeftArmAsReferenceFrame()
    {
        transitionProperty.Adapt(new Layout(transform.position, transform.rotation, CoordinateSystem.LimbLeft));
        if (studyControlPanel.adaptationType == AdaptationType.Adaptive)
        {
            updateObjective.coordinateSystemWhileMoving = CoordinateSystem.LimbLeft;
        }
    }

    public void SelectRightArmAsReferenceFrame()
    {
        transitionProperty.Adapt(new Layout(transform.position, transform.rotation, CoordinateSystem.LimbRight));
        if (studyControlPanel.adaptationType == AdaptationType.Adaptive)
        {
            updateObjective.coordinateSystemWhileMoving = CoordinateSystem.LimbRight;
        }
    }


    public void SelectHeadAsReferenceFrame()
    {
        transitionProperty.Adapt(new Layout(transform.position, transform.rotation, CoordinateSystem.Head));
        if (studyControlPanel.adaptationType == AdaptationType.Adaptive)
        {
            updateObjective.coordinateSystemWhileMoving = CoordinateSystem.Head;
        }
    }

    public void SelectWorldAsReferenceFrame()
    {
        transitionProperty.Adapt(new Layout(transform.position, transform.rotation, CoordinateSystem.World));
    }
    

    // private void EnableSignifier(CoordinateSystem coordinateSystem)
    // {
    //     headAnchoringSignifier.SetActive(coordinateSystem == CoordinateSystem.Head);
    //     torsoAnchoringSignifier.SetActive(coordinateSystem == CoordinateSystem.Torso);
    //     worldAnchoringSignifier.SetActive(coordinateSystem == CoordinateSystem.World);
    //     limbAnchoringSignifier.SetActive(coordinateSystem == CoordinateSystem.LimbLeft);
    // }


}

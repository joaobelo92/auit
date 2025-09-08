using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Objectives;
using AUIT.PropertyTransitions;
using UnityEngine;

public class ReferenceFrameControls : MonoBehaviour
{
    public CoordinateSystemTransition transitionProperty;
    public StudyControlPanel studyControlPanel;
    public List<GameObject> adaptiveSignifiers;

    public GameObject headAnchoringSignifier;
    public GameObject torsoAnchoringSignifier;
    public GameObject limbAnchoringSignifier;
    public GameObject worldAnchoringSignifier;

    public SpriteRenderer limbAnchoringIcon;

    public GameObject worldAnchoringButton;

    public bool LimbLeftAnchoring = false;

    public UpdateCoordinateSystemOnMovement updateObjective;

    // public GameObject buttonUserReferenceFrame;
    // public GameObject buttonWorldReferenceFrame;

    // public GameObject userReferenceFrame;

    void Start()
    {
        foreach (var signifier in adaptiveSignifiers)
        {
            signifier.SetActive(studyControlPanel.adaptationType == AdaptationType.Adaptive);
        }
        EnableSignifier(CoordinateSystem.World);
        if (studyControlPanel.adaptationType == AdaptationType.Adaptive)
        {
            worldAnchoringButton.SetActive(false);
        }
    }

    public void SelectTorsoAsReferenceFrame()
    {
        EnableSignifier(CoordinateSystem.Torso);
        transitionProperty.Adapt(new Layout(transform.position, transform.rotation, CoordinateSystem.Torso));
        LimbLeftAnchoring = false;
        if (studyControlPanel.adaptationType == AdaptationType.Adaptive)
        {
            updateObjective.coordinateSystemWhileMoving = CoordinateSystem.Torso;
        }
    }
    
    public void SelectLimbAsReferenceFrame()
    {
        EnableSignifier(CoordinateSystem.LimbLeft);
        LimbLeftAnchoring = !LimbLeftAnchoring;
        limbAnchoringIcon.flipY = LimbLeftAnchoring;
        CoordinateSystem coordinateSystem = LimbLeftAnchoring ? CoordinateSystem.LimbLeft : CoordinateSystem.LimbRight;
        transitionProperty.Adapt(new Layout(transform.position, transform.rotation, coordinateSystem));
        if (studyControlPanel.adaptationType == AdaptationType.Adaptive)
        {
            updateObjective.coordinateSystemWhileMoving = coordinateSystem;
        }
    }

    public void SelectHeadAsReferenceFrame()
    {
        LimbLeftAnchoring = false;
        EnableSignifier(CoordinateSystem.Head);
        transitionProperty.Adapt(new Layout(transform.position, transform.rotation, CoordinateSystem.Head));
        if (studyControlPanel.adaptationType == AdaptationType.Adaptive)
        {
            updateObjective.coordinateSystemWhileMoving = CoordinateSystem.Head;
        }
    }

    public void SelectWorldAsReferenceFrame()
    {
        LimbLeftAnchoring = false;
        EnableSignifier(CoordinateSystem.World);
        transitionProperty.Adapt(new Layout(transform.position, transform.rotation, CoordinateSystem.World));
    }
    

    private void EnableSignifier(CoordinateSystem coordinateSystem)
    {
        headAnchoringSignifier.SetActive(coordinateSystem == CoordinateSystem.Head);
        torsoAnchoringSignifier.SetActive(coordinateSystem == CoordinateSystem.Torso);
        worldAnchoringSignifier.SetActive(coordinateSystem == CoordinateSystem.World);
        limbAnchoringSignifier.SetActive(coordinateSystem == CoordinateSystem.LimbLeft);
    }


}

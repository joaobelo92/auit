using AUIT.PropertyTransitions;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

public class LoggingHelper : MonoBehaviour
{
    public StudyControlPanel studyControlPanel;

    public string UILoggingName;

    private float manualMovementStartTime;

    public CoordinateSystemTransition coordinateSystemTransition;

    public HandGrabInteractor rightHand;
    public HandGrabInteractor leftHand;

    private bool isMoving = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LogManualMovementStart()
    {
        if ((rightHand.HasSelectedInteractable || leftHand.HasSelectedInteractable) && !isMoving)
        {
            manualMovementStartTime = Time.time;
            studyControlPanel.LogEvent(UILoggingName, "Start Moving", coordinateSystemTransition.CurrentCoordinateSystem, true);
            isMoving = true;
        }
    }

    public void LogManualMovementEnd()
    {
        if (isMoving)
        {
            float manualMovementEndTime = Time.time;
            studyControlPanel.LogEvent(UILoggingName, "Position", coordinateSystemTransition.CurrentCoordinateSystem, false, manualMovementEndTime - manualMovementStartTime);
            studyControlPanel.LogEvent(UILoggingName, "Stop Moving", coordinateSystemTransition.CurrentCoordinateSystem, true);
            isMoving = false;
        }
    }

    public void LogAnchoringChange()
    {
        studyControlPanel.LogEvent(UILoggingName, "AnchorChange", coordinateSystemTransition.CurrentCoordinateSystem, true);
        studyControlPanel.LogEvent(UILoggingName, "AnchorChange", coordinateSystemTransition.CurrentCoordinateSystem, false);
    }
}

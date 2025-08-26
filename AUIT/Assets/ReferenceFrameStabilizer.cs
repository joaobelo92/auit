using System.Collections;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.PropertyTransitions;
using UnityEngine;

public class ReferenceFrameStabilizer : ContextSource<Transform>
{

    public float distanceThreshold = 0.2f;
    public float rotationThreshold = 5f;

    public float animationSpeed = 0.1f;

    public ReferenceFrameUpdater referenceFrameUpdater;

    private Vector3 currentPosition;
    private Quaternion currentRotation;

    public GameObject torsoContextSource;

    public override Transform GetValue()
    {
        return transform;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // if (userContextSource == null && referenceFrameUpdater.upperSpine != null)
        // {
        //     userContextSource = referenceFrameUpdater.upperSpine;
        //     currentPosition = userContextSource.transform.position;
        //     currentRotation = userContextSource.transform.rotation;
        // }
        // else if (userContextSource != null)
        // {
        bool triggerMovement = false;
        Vector3 positionDifference = torsoContextSource.transform.position - currentPosition;
        if (positionDifference.magnitude > distanceThreshold)
        {
            currentPosition = torsoContextSource.transform.position;
            triggerMovement = true;
        }

        if (Quaternion.Angle(torsoContextSource.transform.rotation, currentRotation) > rotationThreshold)
        {
            currentRotation = torsoContextSource.transform.rotation;
            triggerMovement = true;
        }

        if (triggerMovement)
        {
            StartCoroutine(MoveAndRotate(currentPosition, currentRotation, animationSpeed));
        }
        // }
    }

    private IEnumerator MoveAndRotate(Vector3 endPos, Quaternion endRot, float duration)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Smooth easing in/out
            t = Mathf.SmoothStep(0f, 1f, t);

            // Move
            transform.position = Vector3.Lerp(startPos, endPos, t);

            // Rotate
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }

        // Snap to exact target values at the end
        transform.position = endPos;
        transform.rotation = endRot;
    }
}

using System.Collections;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class SmoothMovementTransition : PropertyTransition
    {
        [SerializeField]
        private float movementSpeed = 1.0f;
        [SerializeField]
        private float rotationSpeed = 1.0f;
        [SerializeField]
        private float scalingSpeed = 1.0f;

        public override void Adapt(Layout layout)
        {
            StartCoroutine(SmoothMovement(layout));
        }

        private bool transitionNotDone(float start, float speed)
        {
            return (Time.time - start) * speed < 1f;
        }

        private IEnumerator SmoothMovement(Layout layout)
        {
            float starttime = Time.time;
            Vector3 startPosition = transform.position;
            Vector3 endPosition = layout.Position;
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = layout.Rotation;
            Vector3 startScale = transform.localScale;
            Vector3 endScale = layout.Scale;
            // find minimal speed of all speeds to speed up calculation in while loop
            // sort speeds in ascending order
            float[] speeds = { movementSpeed, rotationSpeed, scalingSpeed };
            System.Array.Sort(speeds);
            // get minimal speed that is not 0
            float minSpeed = 0;
            foreach (float speed in speeds)
            {
                if (speed > 0)
                {
                    minSpeed = speed;
                    break;
                }
            }

            // just run the loop if there is a speed > 0 (otherwise no transition is needed)
            if (minSpeed > 0)
            {
                // run loop while transition is not done
                while (startPosition != endPosition && this.transitionNotDone(starttime, minSpeed))
                {
                    // if movementSpeed > 0 & transition not done, interpolate
                    if (movementSpeed > 0 && this.transitionNotDone(starttime, movementSpeed))
                    {
                        Vector3 resultPosition = Vector3.Lerp(startPosition, endPosition, (Time.time - starttime) * this.movementSpeed);
                        if (!float.IsNaN(resultPosition.x) && !float.IsNaN(resultPosition.y) && !float.IsNaN(resultPosition.z))
                            transform.position = resultPosition;
                    }
                    
                    // if rotationSpeed > 0 & transition not done, interpolate
                    if (rotationSpeed > 0 && this.transitionNotDone(starttime, rotationSpeed))
                    {
                        Quaternion resultRotation = Quaternion.Lerp(startRotation, endRotation, (Time.time - starttime) * this.rotationSpeed);
                        if (!float.IsNaN(resultRotation.x) && !float.IsNaN(resultRotation.y) && !float.IsNaN(resultRotation.z) && !float.IsNaN(resultRotation.w))
                            transform.rotation = resultRotation;
                    }

                    // if scalingSpeed > 0 & transition not done, interpolate
                    if (scalingSpeed > 0 && this.transitionNotDone(starttime, scalingSpeed))
                    {
                        Vector3 resultScale = Vector3.Lerp(startScale, endScale, (Time.time - starttime) * this.scalingSpeed);
                        if (!float.IsNaN(resultScale.x) && !float.IsNaN(resultScale.y) && !float.IsNaN(resultScale.z))
                            transform.localScale = resultScale;
                    }

                    yield return null;
                }
                // AdaptationManager.IsAdapting = false;
            }
        }
    }
}
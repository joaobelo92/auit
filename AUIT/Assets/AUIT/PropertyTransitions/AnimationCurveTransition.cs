using System.Collections;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.PropertyTransitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class AnimationCurveTransition : PropertyTransition
    {
        [SerializeField]
        private AnimationCurve movementCurve;
        [SerializeField]
        private AnimationCurve rotationCurve;
        [SerializeField]
        private AnimationCurve scalingCurve;

        public override void Adapt(Layout layout)
        {
            // if the adaptation is enabled, start the interpolation
            if (this.enabled) {
                StartCoroutine(interpolateLinearly(layout));
            }
        }
        
        private float maxTime(AnimationCurve animationCurve)
        {
            float maxTime = 0.0f;
            int length = animationCurve.length;
            if (length > 0)
            {
                maxTime = animationCurve[length - 1].time;
            }
            return maxTime;
        }

        private IEnumerator interpolateLinearly(Layout layout)
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = layout.Position;
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = layout.Rotation;
            Vector3 startScale = transform.localScale;
            Vector3 endScale = layout.Scale;
            
            float starttime = Time.time;
            float timer = 0.0f;
            
            // run loop while transitions are not done
            while (timer < Mathf.Max(maxTime(movementCurve), maxTime(rotationCurve), maxTime(scalingCurve)))
            {

                Vector3 resultPosition = Vector3.LerpUnclamped(
                    startPosition, 
                    endPosition, 
                    movementCurve.Evaluate(timer)
                    );
                if (!float.IsNaN(resultPosition.x) && !float.IsNaN(resultPosition.y) && !float.IsNaN(resultPosition.z))
                    transform.position = resultPosition;
                
                Quaternion resultRotation = Quaternion.LerpUnclamped(
                    startRotation, 
                    endRotation, 
                    rotationCurve.Evaluate(timer)
                    );
                if (
                    !float.IsNaN(resultRotation.x) && 
                    !float.IsNaN(resultRotation.y) && 
                    !float.IsNaN(resultRotation.z) && 
                    !float.IsNaN(resultRotation.w)
                    )
                    transform.rotation = resultRotation;
                
                Vector3 resultScale = Vector3.LerpUnclamped(startScale, endScale, scalingCurve.Evaluate(timer));
                if (!float.IsNaN(resultScale.x) && !float.IsNaN(resultScale.y) && !float.IsNaN(resultScale.z))
                    transform.localScale = resultScale;

                // update timespan
                timer = Time.time - starttime;
                yield return null;
            }
        }
    }
}
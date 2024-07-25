using System.Collections;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class SmoothMovementTransition : PropertyTransition
    {
        [SerializeField]
        private float transitionSpeed = 1.0f;

        public override void Adapt(Layout layout)
        {
            StartCoroutine(SmoothMovement(layout, transitionSpeed));
        }

        private IEnumerator SmoothMovement(Layout layout, float adaptationSpeed)
        {
            float startime = Time.time;
            Vector3 startPosition = transform.position;
            Vector3 endPosition = layout.Position;
 
            // AdaptationManager.IsAdapting = true;
            while (startPosition != endPosition && (Time.time - startime) * adaptationSpeed < 1f)
            {
                Vector3 result = Vector3.Lerp(startPosition, endPosition, (Time.time - startime) * adaptationSpeed);
                if (!float.IsNaN(result.x) && !float.IsNaN(result.y) && !float.IsNaN(result.z))
                    transform.position = result;

                yield return null;
            }
            // AdaptationManager.IsAdapting = false;
        }
    }
}
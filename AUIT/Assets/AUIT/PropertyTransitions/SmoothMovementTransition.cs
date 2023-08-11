using System.Collections;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class SmoothMovementTransition : PropertyTransition, IPositionAdaptation
    {
        [SerializeField]
        private float transitionSpeed = 1.0f;

        public void Adapt(Transform objectTransform, Vector3 target)
        {
            StartCoroutine(SmoothMovement(objectTransform, target, transitionSpeed));
        }

        public void Adapt(GameObject ui, List<Layout> target)
        {
            throw new System.NotImplementedException();
        }

        private IEnumerator SmoothMovement(Transform objectTransform, Vector3 target, float adaptationSpeed)
        {
            float startime = Time.time;
            Vector3 startPosition = objectTransform.transform.position;
            Vector3 endPosition = target;
 
            // AdaptationManager.IsAdapting = true;
            while (startPosition != endPosition && (Time.time - startime) * adaptationSpeed < 1f)
            {
                Vector3 result = Vector3.Lerp(startPosition, target, (Time.time - startime) * adaptationSpeed);
                if (!float.IsNaN(result.x) && !float.IsNaN(result.y) && !float.IsNaN(result.z))
                    transform.position = result;

                yield return null;
            }
            // AdaptationManager.IsAdapting = false;
        }
    }
}
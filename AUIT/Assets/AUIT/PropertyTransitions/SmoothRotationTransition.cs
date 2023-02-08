using System.Collections;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class SmoothRotationTransition : PropertyTransition, IRotationAdaptation
    {
        [SerializeField]
        private float transitionSpeed = 1.0f;

        public void Adapt(Transform objectTransform, Quaternion target)
        {
            StartCoroutine(SmoothRotation(objectTransform, target, transitionSpeed));
        }

        public void Adapt(GameObject ui, List<Layout> target)
        {
            throw new System.NotImplementedException();
        }

        private IEnumerator SmoothRotation(Transform objectTransform, Quaternion target, float adaptationSpeed)
        {
            float startTime = Time.time;
            Quaternion startRotation = objectTransform.transform.rotation;
            Quaternion endRotation = target;
 
            while (startRotation != endRotation && (Time.time - startTime) * adaptationSpeed < 1f)
            {
                Quaternion result = Quaternion.Lerp(startRotation, target, (Time.time - startTime) * adaptationSpeed);
                transform.rotation = result;

                yield return null;
            }
        }
    }
}
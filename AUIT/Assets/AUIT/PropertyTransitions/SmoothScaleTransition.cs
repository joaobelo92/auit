using System.Collections;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class SmoothScaleTransition : PropertyTransition, IScaleAdaptation
    {
        [SerializeField]
        private float adaptationSpeed = 1.0f;
        
        public void Adapt(Transform objectTransform, Vector3 target)
        {
            StartCoroutine(SmoothScale(objectTransform, target, adaptationSpeed));
        }

        private IEnumerator SmoothScale(Transform objectTransform, Vector3 target, float adaptationSpeed)
        {
            float startTime = Time.time;
            Vector3 startRotation = objectTransform.transform.localScale;
            Vector3 endRotation = target;
 
            while (startRotation != endRotation && (Time.time - startTime) * adaptationSpeed < 1f)
            {
                Vector3 result = Vector3.Lerp(startRotation, target, (Time.time - startTime) * adaptationSpeed);
                transform.localScale = result;

                yield return null;
            }
        }
    }
}
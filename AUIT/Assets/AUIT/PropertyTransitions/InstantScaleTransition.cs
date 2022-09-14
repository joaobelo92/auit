using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class InstantScaleTransition : PropertyTransition, IScaleAdaptation
    {
        public void Adapt(Transform objectTransform, Vector3 target)
        {
            transform.localScale = target;
        }
    }
}
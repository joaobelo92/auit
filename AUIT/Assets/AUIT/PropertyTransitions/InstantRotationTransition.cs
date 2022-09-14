using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class InstantRotationTransition : PropertyTransition, IRotationAdaptation
    {
        public void Adapt(Transform objectTransform, Quaternion target)
        {
            transform.rotation = target;
        }
    }
}
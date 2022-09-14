using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class InstantMovementTransition : PropertyTransition, IPositionAdaptation
    {
        public void Adapt(Transform objectTransform, Vector3 target)
        {
            transform.position = target;
        }
    }
}
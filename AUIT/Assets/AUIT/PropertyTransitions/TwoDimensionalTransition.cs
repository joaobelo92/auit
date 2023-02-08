using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class TwoDimensionalTransition : PropertyTransition, IPositionAdaptation
    {
        [Tooltip("Specify that this object should be a child of the camera.")]
        public bool ShouldBe2D = false;

        private bool is2D = false;

        private Transform originalParent;

        public void Adapt(Transform objectTransform, Vector3 target)
        {
            if (ShouldBe2D && !is2D)
            {
                originalParent = this.transform.parent;
                this.transform.SetParent(Camera.main.transform);
                is2D = true;
            }
            else if (!ShouldBe2D && is2D)
            {
                this.transform.SetParent(originalParent);
                is2D = false;
            }

        }

        public void Adapt(GameObject ui, List<Layout> target)
        {
            throw new System.NotImplementedException();
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class InstantaneousTransition : PropertyTransition
    {
        [SerializeField]
        private bool transformPosition = true;
        [SerializeField]
        private bool transformRotation = true;
        [SerializeField]
        private bool transformScale = true;

        public override void Adapt(Layout layout)
        {
            if (this.enabled) {
                StartCoroutine(transitionInstantaneously(layout));
            }
        }

        private IEnumerator transitionInstantaneously(Layout layout)
        {
            if (transformPosition) {
                transform.position = layout.Position;
            }

            if (transformRotation) {
                transform.rotation = layout.Rotation;
            }

            if (transformScale) {
                transform.localScale = layout.Scale;
            }

            yield return null;
        }
    }
}
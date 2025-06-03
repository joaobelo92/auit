using System.Collections;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    /// <summary>
    /// Smoothly interpolates the local rotation of a GameObject over time using an <see cref="AnimationCurve"/>.
    /// </summary>
    /// <remarks>
    /// The transition uses a quaternion-based interpolation to animate from the current local rotation
    /// to the target rotation defined in the provided <see cref="Layout"/>. The transition follows
    /// a customizable animation curve and duration. Supports transitions in local space.
    /// </remarks>
    public class CurveRotationTransition : PropertyTransition
    {
        /// <summary>
        /// Identifies this transition as a rotation type.
        /// </summary>
        protected override TransitionType TransitionType => TransitionType.Rotation;

        /// <summary>
        /// The animation curve used to control the interpolation speed over time.
        /// </summary>
        public new AnimationCurve animation = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>
        /// The duration of the rotation transition, in seconds.
        /// </summary>
        public float duration = 0.2f;

        private Quaternion _startRotation;
        private Quaternion _targetRotation;

        private bool _adapting;

        /// <summary>
        /// Begins the rotation transition toward the target layout's rotation.
        /// </summary>
        /// <param name="layout">The layout containing the target rotation in world space.</param>
        public override void Adapt(Layout layout)
        {
            if (_adapting) return;

            _startRotation = transform.localRotation;
            _targetRotation = transform.parent
                ? Quaternion.Inverse(transform.parent.rotation) * layout.Rotation
                : layout.Rotation;

            _adapting = true;
            StartCoroutine(AnimateRotation());
        }

        /// <summary>
        /// Coroutine that interpolates the local rotation using <see cref="Quaternion.Slerp"/> and the configured animation curve.
        /// </summary>
        private IEnumerator AnimateRotation()
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float curveT = animation.Evaluate(t);

                transform.localRotation = Quaternion.Slerp(_startRotation, _targetRotation, curveT);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localRotation = _targetRotation;
            _adapting = false;
        }
    }
}

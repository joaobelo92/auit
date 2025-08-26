using System.Collections;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    public class CurvePositionTransition : PropertyTransition
    {
        
        protected override TransitionType TransitionType => TransitionType.Position;
    
        public new AnimationCurve animation = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float duration = 0.2f;
    
        private Vector3 _startPosition;
        private Vector3 _targetPosition;
    
        private bool _adapting;
    
        public override void Adapt(Layout layout)
        {
            if (_adapting) return;
            // Operate in local space to support multiple coordinates systems
            _startPosition = transform.localPosition;
            _targetPosition = layout.CoordinateSystem != CoordinateSystem.World
                ? transform.parent.InverseTransformPoint(layout.Position)
                : layout.Position;

            _adapting = true;
            StartCoroutine(AnimatePosition());
        }
    
        private IEnumerator AnimatePosition()
        {

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float curveT = animation.Evaluate(t);

                transform.localPosition = Vector3.Lerp(_startPosition, _targetPosition, curveT);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = _targetPosition;
            _adapting = false;

        }
    
    }
}

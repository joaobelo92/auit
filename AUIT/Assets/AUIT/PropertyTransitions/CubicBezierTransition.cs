using System.Collections;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.PropertyTransitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    [System.Serializable]
    public class CubicBezier
    {
        [SerializeField]
        private Vector2 point1;
        [SerializeField]
        private Vector2 point2;

        // ensure x values are between 0 and 1 to prevent more than one result for any given t
        public CubicBezier(float p1X, float p1Y, float p2X, float p2Y)
        {
            p1X = Mathf.Min(1, Mathf.Max(0, p1X));
            p2X = Mathf.Min(1, Mathf.Max(0, p2X));
            this.point1 = new Vector2(p1X, p1Y);
            this.point2 = new Vector2(p2X, p2Y);
        }

        public Vector2[] toList()
        {
            return new Vector2[] { this.point1, this.point2 };
        }

        public bool equalsList(Vector2[] list)
        {
            return this.point1 == list[0] && this.point2 == list[1];
        }

        private float cubicBezierFromPointDimensions(float t, float p1, float p2)
        {
            // first part of formula will always be 0 & last part will always be t**3 (since P3 is (1,1))
            return  p1 * 3 * Mathf.Pow((1-t), 2) * t +
                    p2 * 3 * (1-t) * Mathf.Pow(t, 2) +
                    Mathf.Pow(t, 3);
        }

        public float getBezierValue(float time, float totalTime)
        {
            // TODO: implement better (by rearranging the formula)
            // TODO: maybe precompute or improve step algorithm at least
            // !!! VERY INEFFICIENT !!!
            // For now interpolate
            // find the t value that is closest to the time
            float accuracyRange = 0.01f / totalTime;
            float t = 0.5f;
            float nextT = 0.5f;
            float stepsize = 0.5f;
            float currentX = 2.0f;
            while (Mathf.Abs(currentX - time) > accuracyRange)
            {
                t = nextT;
                currentX = this.cubicBezierFromPointDimensions(t, this.point1.x, this.point2.x);
                if (currentX < time)
                {
                    nextT += stepsize;
                }
                else if (currentX > time)
                {
                    nextT -= stepsize;
                }
                stepsize /= 2;
            }
            
            float y = this.cubicBezierFromPointDimensions(t, this.point1.y, this.point2.y);
            return y;
        }
    }

    [System.Serializable]
    public class TransitionModeSelection
    {
        // make a list of selectable options (dropdown menu)
        public enum modes
        {
        Linear,
        Ease,
        EaseInOut,
        EaseIn,
        EaseOut, 
        CubicBezier
        };

        // transition speed
        [SerializeField]
        public float duration = 1.0f;

        // set ease as default
        [SerializeField]
        private modes transitionMode = modes.Ease;
        private modes oldTransitionMode;
        
        // CubicBezierTransition
        [SerializeField]
        private CubicBezier transitionBezier;
        private Vector2[] oldTransitionBezier;

        // initialize Object with default values
        public TransitionModeSelection()
        {
            this.transitionBezier = modeToBezier(this.transitionMode);
            updateOldValues();
        }

        // callback to when some values might have changed
        public void OnValidate()
        {
            // if the transition mode has changed, update the bezier object
            if (this.transitionMode != this.oldTransitionMode)
            {
                this.transitionBezier = modeToBezier(this.transitionMode);
                updateOldValues();
            }

            // if the bezier object has changed, update the transition mode
            if (!this.transitionBezier.equalsList(this.oldTransitionBezier))
            {
                this.transitionMode = bezierToMode(this.transitionBezier);
                updateOldValues();
            }
        }

        // update the old values
        private void updateOldValues()
        {
            this.oldTransitionMode = this.transitionMode;
            this.oldTransitionBezier = this.transitionBezier.toList();
        }

        private bool isLinearBezier(Vector2[] bezierPoints)
        {
            return bezierPoints[0].x == bezierPoints[0].y && bezierPoints[1].x == bezierPoints[1].y;
        }

        // convert CubicBezier object to mode
        private modes bezierToMode(CubicBezier bezier)
        {
            Vector2[] bezierList = bezier.toList();

            // if first point is (0.42,0)
            if (bezierList[0].x == 0.42f && bezierList[0].y == 0.0f)
            {
                // if second point is (1,1)
                if (bezierList[1].x == 1.0f && bezierList[1].y == 1.0f)
                {
                    return modes.EaseIn;
                }
                // if second point is (0.58,1)
                else if (bezierList[1].x == 0.58f && bezierList[1].y == 1.0f)
                {
                    return modes.EaseInOut;
                }
                return modes.CubicBezier;
            }

            // if first point is (0.25,0.1) and second point is (0.25,1)
            if (bezierList[0].x == 0.25f && bezierList[0].y == 0.1f && bezierList[1].x == 0.25f && bezierList[1].y == 1.0f)
            {
                return modes.Ease;
            }

            // if first point is (0,0) and the second point is (0.58,1)
            if (bezierList[0].x == 0.0f && bezierList[0].y == 0.0f && bezierList[1].x == 0.58f && bezierList[1].y == 1.0f)
            {
                return modes.EaseOut;
            }

            // check if the bezier curve is linear
            if (isLinearBezier(bezierList))
            {
                return modes.Linear;    
            }

            // else return CubicBezier
            return modes.CubicBezier;
        }
        

        // convert mode to CubicBezier object
        private CubicBezier modeToBezier(modes mode)
        {
            switch (mode)
            {
                case modes.Linear:
                    return new CubicBezier(0.0f, 0.0f, 1.0f, 1.0f);
                case modes.Ease:
                    return new CubicBezier(0.25f, 0.1f, 0.25f, 1.0f);
                case modes.EaseInOut:
                    return new CubicBezier(0.42f, 0.0f, 0.58f, 1.0f);
                case modes.EaseIn:
                    return new CubicBezier(0.42f, 0.0f, 1.0f, 1.0f);
                case modes.EaseOut:
                    return new CubicBezier(0.0f, 0.0f, 0.58f, 1.0f);
                case modes.CubicBezier:
                    return new CubicBezier(0.79f, 0.33f, 0.14f, 0.53f);
                default:
                    return modeToBezier(modes.Ease);
            }
        }

        // get a value from 0-1 that represents the transition progress
        public float timespanToTransitionValue(float timespan)
        {
            // if duration is 0, transition instantly
            if (this.duration == 0)
            {
                return 1;
            }

            // a point from 0-1, that is used to calculate the transition value
            float timepoint = timespan / this.duration;
            // timepoint is min 0 and max 1
            timepoint = Mathf.Min(1, Mathf.Max(0, timepoint));

            // calculate the transition value based on the current Bezier curve
            // to improve performance, we give accurace TODO: remove when algorithm is improved
            return this.transitionBezier.getBezierValue(timepoint, timespan);
        }
    }

    public class CubicBezierTransition : PropertyTransition
    {
        [SerializeField]
        private TransitionModeSelection movementBezier = new TransitionModeSelection();
        [SerializeField]
        private TransitionModeSelection rotationBezier = new TransitionModeSelection();
        [SerializeField]
        private TransitionModeSelection scalingBezier = new TransitionModeSelection();

        // if the values are updated, relay the changes to the bezier objects
        private void OnValidate()
        {
            movementBezier.OnValidate();
            rotationBezier.OnValidate();
            scalingBezier.OnValidate();
        }

        public override void Adapt(Layout layout)
        {
            // if the adaptation is enabled, start the interpolation
            if (this.enabled) {
                StartCoroutine(interpolateLinearly(layout));
            }
        }

        private IEnumerator interpolateLinearly(Layout layout)
        {
            float starttime = Time.time;
            Vector3 startPosition = transform.position;
            Vector3 endPosition = layout.Position;
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = layout.Rotation;
            Vector3 startScale = transform.localScale;
            Vector3 endScale = layout.Scale;
            float timespan = 0;

            // run loop while transitions are not done
            while (startPosition != endPosition || startRotation != endRotation || startScale != endScale)
            {
                // if movementSpeed > 0 & transition not done, interpolate
                if (movementBezier.duration > 0 && timespan < movementBezier.duration)
                {
                    Vector3 resultPosition = Vector3.Lerp(startPosition, endPosition, movementBezier.timespanToTransitionValue(timespan));
                    if (!float.IsNaN(resultPosition.x) && !float.IsNaN(resultPosition.y) && !float.IsNaN(resultPosition.z))
                        transform.position = resultPosition;
                }

                // if rotationSpeed > 0 & transition not done, interpolate
                if (rotationBezier.duration > 0 && timespan < rotationBezier.duration)
                {
                    Quaternion resultRotation = Quaternion.Lerp(startRotation, endRotation, rotationBezier.timespanToTransitionValue(timespan));
                    if (!float.IsNaN(resultRotation.x) && !float.IsNaN(resultRotation.y) && !float.IsNaN(resultRotation.z) && !float.IsNaN(resultRotation.w))
                        transform.rotation = resultRotation;
                }

                // if scalingSpeed > 0 & transition not done, interpolate
                if (scalingBezier.duration > 0 && timespan < scalingBezier.duration)
                {
                    Vector3 resultScale = Vector3.Lerp(startScale, endScale, scalingBezier.timespanToTransitionValue(timespan));
                    if (!float.IsNaN(resultScale.x) && !float.IsNaN(resultScale.y) && !float.IsNaN(resultScale.z))
                        transform.localScale = resultScale;
                }

                // update timespan
                timespan = Time.time - starttime;
                yield return null;
            }
        }
    }
}
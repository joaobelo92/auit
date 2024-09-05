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
        [Tooltip("The first control point of the cubic bezier curve.\n" +
                 "The x value should be between (including) 0 and 1.")]
        [SerializeField]
        private Vector2 point1;

        [Tooltip("The second control point of the cubic bezier curve.\n" +
                 "The x value should be between (including) 0 and 1.")]
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

        // TODO: optimize more by rearranging the formula
        private float cubicBezierFromPointDimensions(float t, float p1, float p2)
        {
            // first part of formula will always be 0 & last part will always be t**3 (since P3 is (1,1))
            return  p1 * 3 * Mathf.Pow((1-t), 2) * t +
                    p2 * 3 * (1-t) * Mathf.Pow(t, 2) +
                    Mathf.Pow(t, 3);
        }

        // TODO: optimize more by rearranging the formula
        private float calculateTangent(float t, float p1, float p2)
        {
            // calculate the tangent of the curve at the given t value (1st derivate)
            return  3 * (p1 - (4 * p1 + 3 * t * p1) * t) +
                    3 * t * (2 * p2 - 3 * t * p2) +
                    3 * t * t;
        }

        private float newtonsMethodToFindT(float time, float totalTime) {
            // use Newton's method to find the t value that gives the closest x value to the time
            // https://en.wikipedia.org/wiki/Newton's_method
            // the minimal accuracy for the calculated time value
            float accuracy = 0.01f / totalTime;
            // the initial t value, choose current time value as approximation
            float approximatedT = time;
            // the difference between the time and calculated x value (max 1)
            float diffTimeX = 1.0f;
            // to prevent infinite loops, set a counter
            // WARNING: limits the accuracy of the result (for very high total times)
            int counter = 0;
            while (Mathf.Abs(diffTimeX) > accuracy && counter < 100)
            {
                counter++;
                // calculate the tangent of the curve at the current t value
                float tangent = Mathf.Abs(this.calculateTangent(approximatedT, this.point1.x, this.point2.x));
                // if the tangent is 0, we have found the correct t value
                if (tangent == 0)
                {
                    break;
                }
                // calculate the bezier value at the current t value
                diffTimeX = this.cubicBezierFromPointDimensions(approximatedT, this.point1.x, this.point2.x) - time;
                Debug.Log("diffTimeX: " + diffTimeX + " time: " + time + " t: " + approximatedT + " tangent: " + tangent);
                // calculate the new t value based on the tangent
                approximatedT -= diffTimeX / tangent;
                // DEBUG
                if (approximatedT > 1 || approximatedT < 0)
                {
                    Debug.Log("Newton's method failed, t value out of bounds: " + approximatedT);
                    break;
                }
            }
            Debug.Log("Newton's method iterations: " + counter + " with diffTimeX: " + diffTimeX + " and found t: " + approximatedT);
            return approximatedT;
        }

        // TODO: replace with newtons method
        private float stupidMethodToFindT(float time, float totalTime)
        {
            float accuracyRange = 0.01f / totalTime;
            float t = 0.5f;
            float stepsize = 0.5f;
            float currentX = time;
            int i = 0;
            do
            {
                i++;
                if (currentX < time)
                {
                    t += stepsize;
                }
                else if (currentX > time)
                {
                    t -= stepsize;
                }
                currentX = this.cubicBezierFromPointDimensions(t, this.point1.x, this.point2.x);
                stepsize /= 2;
            } while (Mathf.Abs(currentX - time) > accuracyRange);
            return t;
        }

        private float firefoxMethodToFindT(float time, float totalTime)
        {
            float A(float aA1, float aA2) { return 1.0f - 3.0f * aA2 + 3.0f * aA1; }
            float B(float aA1, float aA2) { return 3.0f * aA2 - 6.0f * aA1; }
            float C(float aA1)  { return 3.0f * aA1; }

            // Returns x(t) given t, x1, and x2, or y(t) given t, y1, and y2.
            float CalcBezier(float aT, float aA1, float aA2) {
                return ((A(aA1, aA2)*aT + B(aA1, aA2))*aT + C(aA1))*aT;
            }

            // Returns dx/dt given t, x1, and x2, or dy/dt given t, y1, and y2.
            float GetSlope(float aT, float aA1, float aA2) {
                return 3.0f * A(aA1, aA2)*aT*aT + 2.0f * B(aA1, aA2) * aT + C(aA1);
            }

            float GetTForX(float aX) {
                // Newton raphson iteration
                var aGuessT = aX;
                var currentX = 0.0f;
                for (var i = 0; i < 4; ++i) {
                    var currentSlope = GetSlope(aGuessT, this.point1.x, this.point2.x);
                    if (currentSlope == 0.0) return aGuessT;
                    currentX = CalcBezier(aGuessT, this.point1.x, this.point2.x) - aX;
                    aGuessT -= currentX / currentSlope;
                }
                Debug.Log("currentX: " + currentX);
                return aGuessT;
            }

            return GetTForX(time);
        }

        // TODO: maybe improve performance with precalculated lookup table
        public float getBezierValue(float time, float totalTime)
        {
            // test for linear bezier to improve performance
            if (this.point1.x == this.point1.y && this.point2.x == this.point2.y)
            {
                return time;
            }
            // if not linear, do normal bezier calculation
            float t = this.stupidMethodToFindT(time, totalTime);   
            float y = this.cubicBezierFromPointDimensions(t, this.point1.y, this.point2.y);
            Debug.Log("t: " + t + " y: " + y + " time: " + time + " point1: " + this.point1 + " point2: " + this.point2);
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
        [Tooltip("The duration of the transition in seconds.\n" +
                 "If set to 0, the transition will be instant.")]
        [SerializeField]
        public float duration = 1.0f;

        // set ease as default
        [Tooltip("The transition mode to use (similar to e.g. CSS transition modes).")]
        [SerializeField]
        private modes transitionMode = modes.Ease;
        private modes oldTransitionMode;
        
        // CubicBezierTransition
        [Tooltip("The Cubic Bezier defining the transition curve.")]
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
            // if the bezier object has changed, update the transition mode
            if (!this.transitionBezier.equalsList(this.oldTransitionBezier))
            {
                this.transitionMode = bezierToMode(this.transitionBezier);
                updateOldValues();
            }

            // if the transition mode has changed, update the bezier object
            if (this.transitionMode != this.oldTransitionMode)
            {
                this.transitionBezier = modeToBezier(this.transitionMode);
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
            // if duration is <= 0, transition instantly
            if (this.duration <= 0)
            {
                return 1;
            }

            // a point from 0-1, that is used to calculate the transition value
            float timepoint = timespan / this.duration;
            // timepoint is min 0 and max 1
            timepoint = Mathf.Min(1, Mathf.Max(0, timepoint));

            // calculate the transition value based on the current Bezier curve
            return this.transitionBezier.getBezierValue(timepoint, this.duration);
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
            bool posDone = false;
            bool rotDone = false;
            bool scaleDone = false;
            float timespan = 0;

            // run loop while transitions are not done
            while (!posDone || !rotDone || !scaleDone)
            {
                // if movementSpeed > 0 & transition not done, interpolate
                if (movementBezier.duration > 0 && timespan < movementBezier.duration)
                {
                    Vector3 resultPosition = Vector3.LerpUnclamped(startPosition, endPosition, movementBezier.timespanToTransitionValue(timespan));
                    Debug.Log(resultPosition);
                    if (!float.IsNaN(resultPosition.x) && !float.IsNaN(resultPosition.y) && !float.IsNaN(resultPosition.z))
                        transform.position = resultPosition;
                } else if (timespan >= movementBezier.duration && !posDone)
                {
                    transform.position = endPosition;
                    posDone = true;
                }

                // if rotationSpeed > 0 & transition not done, interpolate
                if (rotationBezier.duration > 0 && timespan < rotationBezier.duration)
                {
                    Quaternion resultRotation = Quaternion.LerpUnclamped(startRotation, endRotation, rotationBezier.timespanToTransitionValue(timespan));
                    if (!float.IsNaN(resultRotation.x) && !float.IsNaN(resultRotation.y) && !float.IsNaN(resultRotation.z) && !float.IsNaN(resultRotation.w))
                        transform.rotation = resultRotation;
                } else if (timespan >= rotationBezier.duration && !rotDone)
                {
                    transform.rotation = endRotation;
                    rotDone = true;
                }

                // if scalingSpeed > 0 & transition not done, interpolate
                if (scalingBezier.duration > 0 && timespan < scalingBezier.duration)
                {
                    Vector3 resultScale = Vector3.LerpUnclamped(startScale, endScale, scalingBezier.timespanToTransitionValue(timespan));
                    if (!float.IsNaN(resultScale.x) && !float.IsNaN(resultScale.y) && !float.IsNaN(resultScale.z))
                        transform.localScale = resultScale;
                } else if (timespan >= scalingBezier.duration && !scaleDone)
                {
                    transform.localScale = endScale;
                    scaleDone = true;
                }

                // update timespan
                timespan = Time.time - starttime;
                yield return null;
            }
        }
    }
}
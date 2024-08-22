using System.Collections;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.PropertyTransitions;
using UnityEngine;

namespace AUIT.PropertyTransitions
{
    [System.Serializable]
    public class BezierPoint
    {
        [SerializeField]
        private float x;
        [SerializeField]
        private float y;

        public BezierPoint(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float[] toList()
        {
            return new float[] { this.x, this.y };
        }

        public bool equalsList(float[] list)
        {
            return this.x == list[0] && this.y == list[1];
        }
    }

    [System.Serializable]
    public class CubicBezier
    {
        [SerializeField]
        private BezierPoint point1;
        [SerializeField]
        private BezierPoint point2;

        public CubicBezier(float p1X, float p1Y, float p2X, float p2Y)
        {
            this.point1 = new BezierPoint(p1X, p1Y);
            this.point2 = new BezierPoint(p2X, p2Y);
        }

        public float[][] toList()
        {
            return new float[][] { this.point1.toList(), this.point2.toList() };
        }

        public bool equalsList(float[][] list)
        {
            return this.point1.equalsList(list[0]) && this.point2.equalsList(list[1]);
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
        private float speed = 1.0f;

        // set ease as default
        [SerializeField]
        private modes transitionMode = modes.Ease;
        private modes oldTransitionMode;
        
        // CubicBezierTransition
        [SerializeField]
        private CubicBezier transitionBezier;
        private float[][] oldTransitionBezier;

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

        // convert CubicBezier object to mode
        private modes bezierToMode(CubicBezier bezier)
        {
            float[][] bezierList = bezier.toList();
            // if first point is (0,0)
            if (bezierList[0][0] == 0.0f && bezierList[0][1] == 0.0f)
            {
                // if second point is (1,1)
                if (bezierList[1][0] == 1.0f && bezierList[1][1] == 1.0f)
                {
                    return modes.Linear;
                }
                // if second point is (0.58,1)
                else if (bezierList[1][0] == 0.58f && bezierList[1][1] == 1.0f)
                {
                    return modes.EaseOut;
                }
                return modes.CubicBezier;
            }

            // if first point is (0.42,0)
            if (bezierList[0][0] == 0.42f && bezierList[0][1] == 0.0f)
            {
                // if second point is (1,1)
                if (bezierList[1][0] == 1.0f && bezierList[1][1] == 1.0f)
                {
                    return modes.EaseIn;
                }
                // if second point is (0.58,1)
                else if (bezierList[1][0] == 0.58f && bezierList[1][1] == 1.0f)
                {
                    return modes.EaseInOut;
                }
                return modes.CubicBezier;
            }

            // if first point is (0.25,0.1) and second point is (0.25,1)
            if (bezierList[0][0] == 0.25f && bezierList[0][1] == 0.1f && bezierList[1][0] == 0.25f && bezierList[1][1] == 1.0f)
            {
                return modes.Ease;
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

        private bool transitionNotDone(float start, float speed)
        {
            return (Time.time - start) * speed < 1f;
        }

        private IEnumerator interpolateLinearly(Layout layout)
        {
            yield return null;
            // float starttime = Time.time;
            // Vector3 startPosition = transform.position;
            // Vector3 endPosition = layout.Position;
            // Quaternion startRotation = transform.rotation;
            // Quaternion endRotation = layout.Rotation;
            // Vector3 startScale = transform.localScale;
            // Vector3 endScale = layout.Scale;
            // // find minimal speed of all speeds to speed up calculation in while loop
            // // sort speeds in ascending order
            // float[] speeds = { movementSpeed, rotationSpeed, scalingSpeed };
            // System.Array.Sort(speeds);
            // // get minimal speed that is not 0
            // float minSpeed = 0;
            // foreach (float speed in speeds)
            // {
            //     if (speed > 0)
            //     {
            //         minSpeed = speed;
            //         break;
            //     }
            // }

            // // just run the loop if there is a speed > 0 (otherwise no transition is needed)
        }
    }
}
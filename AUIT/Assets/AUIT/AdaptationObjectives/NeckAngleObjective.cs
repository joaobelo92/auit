using AUIT.AdaptationObjectives.Definitions;
using AUIT.AdaptationObjectives.Extras;
using UnityEngine;

using Random = UnityEngine.Random;

namespace AUIT.AdaptationObjectives
{
    /// <summary>
    /// This objective is used to minimize the angle between the vector from the
    /// eye position to the GameObject and the vector from the eye position to the
    /// target's projection on the xz-plane at the eye position's height.
    /// It is a simplified neck ergonomics objective based on RULA.
    /// </summary>
    public class NeckAngleObjective : LocalObjective
    {
                
        public void Reset()
        {
            ContextSource = ContextSource.PlayerPose;
        }

        protected override void Start()
        {
            base.Start();
            if (ContextSource == ContextSource.Gaze)
            {
                ContextSource = ContextSource.PlayerPose;
            }
        }
        
        private float GetNormalizedNeckAngle(Vector3 targetPosition, Vector3 currentEyePosition)
        {
            // Get the vector from the eye position to the target.
            Vector3 eyeToTarget = targetPosition - currentEyePosition;

            // Get the vector from the eye position to the target's projection on the xz-plane
            // at the eye position's height.
            // Vector3 eyeToTargetProjection =
            //     new Vector3(eyeToTarget.x, currentEyePosition.y, eyeToTarget.z);
            Vector3 eyeToTargetProjection =
                new Vector3(eyeToTarget.x, 0, eyeToTarget.z);

            // Get the angle between the two vectors.
            float angle = Vector3.Angle(eyeToTarget, eyeToTargetProjection);

            // Normalize the angle to the interval [0, 1].
            float normalizedAngle = angle / 90;

            return normalizedAngle;
        }

        /// <summary>
        /// Returns the neck angle normalized to the interval [0, 1].
        /// A neck angle of 0 means that the target is at eye level of the user but not at the eye position.
        /// A neck angle of 1 means that the target is right below the user's head (i.e.,
        /// the user has to look down at a 90 degree angle to see the target), right above
        /// the user's head (i.e., the user has to look up at a 90 degree angle to see the target),
        /// or on the user's eye position.
        /// A neck angle of 0.5 means that the target is at a 45 degree angle in front of the user,
        /// either above or below the user's head.
        /// </summary>
        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            Vector3 currentEyePosition = (Vector3) ContextSourceTransformTarget;
            Vector3 targetPosition = optimizationTarget.Position; // This is technically the camera position.

            float normalizedAngle =
                GetNormalizedNeckAngle(targetPosition, currentEyePosition);

            return normalizedAngle;
        }

        public override Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout)
        {
            // Do nothing for now.
            return optimizationTarget;
        }

        public override Layout DirectRule(Layout optimizationTarget)
        {
            // Do nothing for now.
            return optimizationTarget;
        }
    }
}

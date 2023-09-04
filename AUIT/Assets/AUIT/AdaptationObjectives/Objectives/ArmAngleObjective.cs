using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.AdaptationObjectives.Objectives
{
    /// <summary>
    /// This objective is used to minimize the angle between the vector from the
    /// shoulder position straight down to the ground (i.e., resting arm position)
    /// and the vector from the shoulder position to the target.
    /// It is a simplified arm/shoulder ergonomics objective based on RULA.
    /// </summary>
    public class ArmAngleObjective : LocalObjective
    {
        [SerializeField]
        private float eyeToShoulderDistance = 0.25f;
        
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

        private float GetNormalizedArmAngle(Vector3 targetPosition, Vector3 shoulderPosition)
        {
            // If the target is on the shoulder position, we return 1.
            if (targetPosition == shoulderPosition)
            {
                return 1.0f;
            }

            // Get the vector from the shoulder position to the target.
            Vector3 shoulderToTarget = targetPosition - shoulderPosition;

            // Get the vector from the shoulder position straight down to the ground.
            // Vector3 shoulderToGround = new Vector3(shoulderPosition.x, -1, shoulderPosition.z);
            Vector3 shoulderToGround = new Vector3(0, -1, 0);

            // Get the angle between the two vectors.
            float angle = Vector3.Angle(shoulderToTarget, shoulderToGround);

            // Normalize the angle to the interval [0, 1].
            float normalizedAngle = angle / 180;

            return normalizedAngle;
        }

        /// <summary>
        /// Returns the arm angle normalized to the interval [0, 1].
        /// An arm angle of 0 means that the target is right below the shoulder joint (i.e.,
        /// the user can interact with the target without lifting the arm).
        /// An arm angle of 1 means that the target is on or right above the shoulder joint (i.e.,
        /// the user has to lift the arm fully to interact with the target).
        /// An arm angle of 0.5 means that the target is right at the level of the shoulder joint.
        /// WARN: These are not realistic scenarios, but are used to test the objective.
        /// Whether the user can interact with the target depends on the target's size and
        /// the user's arm length.
        /// </summary>
        public override float CostFunction(Layout optimizationTarget, Layout initialLayout = null)
        {
            Vector3 currentPosition = (Vector3)ContextSourceTransformTarget;
            Vector3 targetPosition = optimizationTarget.Position;

            // Calculate the shoulder position (the shoulder is eyeToShoulderDistance below the eye)
            Vector3 shoulderPosition = currentPosition;
            shoulderPosition.y -= eyeToShoulderDistance;

            // Calculate the normalized arm angle
            float normalizedAngle = GetNormalizedArmAngle(targetPosition, shoulderPosition);

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
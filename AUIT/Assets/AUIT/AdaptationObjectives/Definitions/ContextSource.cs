namespace AUIT.AdaptationObjectives.Definitions
{
    public enum ContextSource
    {
        /// <summary>
        /// Main camera (gaze) is the source of context.
        /// </summary>
        Gaze = 0,
        /// <summary>
        /// Pose of the player is the source of context (trivial starting simplification is the Main camera position).
        /// </summary>
        PlayerPose = 1,
        /// <summary>
        /// Transform of choice is the source of context
        /// </summary>
        CustomTransform = 2,
        
        
        // TODO: Add others
        /*
        /// <summary>
        /// Hand transform is the source of context
        /// </summary>
        HandJoint = 2,
        ///<summary>
        /// System-calculated ray of available controller is the source of context
        /// </summary>
        ControllerRay = 2,
        */
    }
}
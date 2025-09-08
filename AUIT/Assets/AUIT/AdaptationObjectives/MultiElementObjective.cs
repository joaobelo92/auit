using System;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public abstract class MultiElementObjective : MonoBehaviour
    {
        
        
        protected AUIT auit;

        [SerializeField]
        [Range(0, 1)]
        protected float weight = 0.5f;
        public float Weight { get { return weight; } set { weight = value; } }

        #region MonoBehaviour Implementation

        protected virtual void OnEnable()
        {
            if (auit == null)
                auit = FindFirstObjectByType<AUIT>();
            auit.RegisterMultiElementObjective(this);
        }

        protected virtual void Start() 
        {
            if (auit == null)
                auit = FindFirstObjectByType<AUIT>();
        }

        protected virtual void OnDisable()
        {
            if (auit != null)
                auit.UnregisterMultiElementObjective(this);
        }

        #endregion
        
        public abstract float CostFunction(Layout[] optimizationTargets, Layout initialLayout = null);

        public abstract List<Layout> OptimizationRule(List<Layout> optimizationTargets, Layout initialLayout = null);

        public ContextSource<Camera> GetUserCameraContextSource()
        {
            // Search scene for GameObject "User Pose" with a TransformContextSourceComponent

            ContextSource<Camera> userPoseContextSource = null;
            GameObject userPose = GameObject.Find("User Camera");
            if (userPose != null)
            {
                userPoseContextSource = userPose.GetComponent<ContextSource<Camera>>();
            }
            return userPoseContextSource;
        }

        public abstract float[] GetParameters();

        public abstract void SetParameters(float[] parameters);
    }
}
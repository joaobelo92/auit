using System;
using AUIT.AdaptationObjectives.Definitions;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace AUIT.AdaptationObjectives
{

    public enum ObjectiveType
    {
        AvoidOcclusion,
        TargetDistance,
        FieldOfView,
        UpdateCoordinateSystemOnMovement,
        LookTowards,
        PreferPreviousPositions,
        NotSpecified,
    }

    // Local Objectives have to derive from MonoBehaviour, so I'm unsure an interface for Global and Local is possible.
    [RequireComponent(typeof(LocalObjectiveHandler))]
    public abstract class LocalObjective : MonoBehaviour
    {
        #region Context Source Logic

        protected LocalObjectiveHandler ObjectiveHandler;

        [HideInInspector]
        public abstract ObjectiveType ObjectiveType { get; }

        public OptimizationTarget OptimizationTarget { get; set; } = OptimizationTarget.Position;

        [SerializeField]
        [Range(0, 1)]
        protected float weight = 0.5f;
        public float Weight { get { return weight; } set { weight = value; } }

        public abstract float CostFunction(Layout optimizationTarget, Layout initialLayout = null);

        /// <summary>
        /// We will want to apply rules with different odds. Still need to figure out if that should be defined
        /// per rule and hardcoded or not.
        /// For some objectives we know how to compute the ground truth for the optimal values in regards to a
        /// particular loss function, perhaps that should be also in the objectives for greedy optimization approaches
        /// </summary>
        /// <param name="optimizationTarget"></param>
        /// <returns></returns>
        public abstract Layout OptimizationRule(Layout optimizationTarget, Layout initialLayout = null);

        /// <summary>
        /// For now, greedy rules are optional. 
        /// </summary>
        /// <param name="optimizationTarget"></param>
        /// <returns></returns>
        public abstract Layout DirectRule(Layout optimizationTarget);

        public ContextSource<Transform> GetUserPoseContextSource()
        {
            // Search scene for GameObject "User Pose" with a TransformContextSourceComponent
            
            ContextSource<Transform> userPoseContextSource = null;
            GameObject userPose = GameObject.Find("User Pose");
            if (userPose != null)
            {
                userPoseContextSource = userPose.GetComponent<ContextSource<Transform>>();
            }
            return userPoseContextSource;
        }

        public abstract float[] GetParameters();

        public abstract void SetParameters(float[] parameters);

        public GameObject TargetGameObject => gameObject;

        #endregion

        #region MonoBehaviour Implementation

        protected virtual void Awake()
        {
            
        }


        protected virtual void OnEnable()
        {
            if (ObjectiveHandler == null)
                ObjectiveHandler = GetComponent<LocalObjectiveHandler>();
            ObjectiveHandler.RegisterObjective(this);
        }

        protected virtual void Start()
        {
            if (ObjectiveHandler == null)
                ObjectiveHandler = GetComponent<LocalObjectiveHandler>();
        }

        protected virtual void OnDisable()
        {
            if (ObjectiveHandler != null)
                ObjectiveHandler.UnregisterObjective(this);
        }

        #endregion




    }
}

using System;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.Constraints
{
    public enum ConstraintType
    {
        SpatialXAxis,
        SpatialYAxis,
        SpatialZAxis,
    }
    
    [Serializable]
    public class Constraint
    {

        public ConstraintType type;
        
        public float minimum;
        public float maximum;

        [SerializeField]
        [Tooltip("Only used if Context Source is set to Custom Transform.")]
        protected Transform transformOverride;

    }   
}
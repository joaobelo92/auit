using System;
using System.Collections.Generic;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public abstract class MultiElementObjective : MonoBehaviour
    {
        
        
        protected AUIT auit;
        
        #region MonoBehaviour Implementation

        protected virtual void Awake()
        {
            if (auit == null)
                auit = GetComponent<AUIT>();
        }

        protected virtual void OnEnable()
        {
            if (auit == null)
                auit = GetComponent<AUIT>();
            auit.RegisterMultiElementObjective(this);
        }

        protected virtual void Start()
        {
        }

        protected virtual void OnDisable()
        {
            if (auit != null)
                auit.UnregisterMultiElementObjective(this);
        }

        #endregion
        
        public abstract float CostFunction(Layout[] optimizationTargets, Layout initialLayout = null);

        public abstract List<Layout> OptimizationRule(List<Layout> optimizationTarget, Layout initialLayout = null);
    }
}
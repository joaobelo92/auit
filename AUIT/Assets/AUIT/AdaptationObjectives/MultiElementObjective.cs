using System;
using AUIT.AdaptationObjectives.Definitions;
using UnityEngine;

namespace AUIT.AdaptationObjectives
{
    public abstract class MultiElementObjective : LocalObjective
    {
        
        public abstract float CostFunction(Layout[] optimizationTargets, Layout initialLayout = null);

        public abstract Layout OptimizationRule(Layout[] optimizationTarget, Layout initialLayout = null);
    }
}
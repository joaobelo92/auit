using System;
using System.Collections.Generic;
using UnityEngine;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using AUIT.AdaptationObjectives;
using AUIT.Constraints;
using Numpy;


namespace AUIT.Solvers
{
    [Serializable]
    public abstract class IAsyncSolver
    {
        public virtual void Initialize(List<Constraint> constraints=null) {}
        public virtual void Destroy() {}
        // TODO: initialize objectives and constraints once
        public abstract UniTask<(OptimizationResponse, NDarray, NDarray)> OptimizeCoroutine(
            List<Layout> initialLayouts,
            List<List<LocalObjective>> objectives,
            List<MultiElementObjective> multiElementObjectives,
            bool saveCosts=false
        );
        public AUIT Auit { set; get; } 
    }
}
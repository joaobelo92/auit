using System.Collections.Generic;
using UnityEngine;
using AUIT.AdaptationObjectives.Definitions;
using AUIT.Extras;
using Cysharp.Threading.Tasks;
using AUIT.AdaptationObjectives;
using AUIT.Constraints;


namespace AUIT.Solvers
{
    [System.Serializable]
    public abstract class IAsyncSolver
    {
        [SerializeReference]
        public List<Constraint> constraints;
        public virtual void Initialize() {}
        public virtual void Destroy() {}
        public abstract UniTask<OptimizationResponse> OptimizeCoroutine(
            List<Layout> initialLayouts,
            List<List<LocalObjective>> objectives
        );
        public AUIT Auit { set; get; } 
    }
}
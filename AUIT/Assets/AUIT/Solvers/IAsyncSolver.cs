using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using Cysharp.Threading.Tasks;

namespace AUIT.Solvers
{
    public abstract class IAsyncSolver
    { 
        public void Initialize() {}
        public void Destroy() {}
        public abstract UniTask<(List<List<Layout>>, float)> OptimizeCoroutine(
            List<Layout> initialLayouts,
            List<List<LocalObjective>> objectives
        );
    }
}
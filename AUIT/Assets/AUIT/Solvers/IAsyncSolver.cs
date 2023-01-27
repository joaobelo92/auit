using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;

namespace AUIT.Solvers.Experimental
{
    public interface IAsyncSolver
    {
        AdaptationManager adaptationManager { set; }
        (List<Layout>, float, float) Result { get; }
        
        void Initialize();
        IEnumerator OptimizeCoroutine(Layout initialLayout, List<LocalObjective> objectives, List<float> hyperparameters);
        IEnumerator OptimizeCoroutine(List<Layout> initialLayouts, List<List<LocalObjective>> objectives, List<float> hyperparameters);
    }
}
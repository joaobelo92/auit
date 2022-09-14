using System.Collections.Generic;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;

namespace AUIT.Solvers
{
    public interface ISolver
    {
        (List<Layout>, float) Optimize(Layout initialLayout, List<LocalObjective> objectives, List<float> hyperparameters);
        (List<Layout>, float) Optimize(List<Layout> initialLayouts, List<List<LocalObjective>> objectives, List<float> hyperparameters);
    }
}
using AUIT.AdaptationObjectives.Definitions;

namespace AUIT.AdaptationTriggers
{
    public interface AdaptationListener
    {
        void AdaptationUpdated(Layout adaptation);
    }
}
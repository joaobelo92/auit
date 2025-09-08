using AUIT.AdaptationTriggers;
using AUIT.PropertyTransitions;

public class CoordinateSystemContextSource : ContextSource<CoordinateSystemTransition>
{
    public override CoordinateSystemTransition GetValue()
    {
        return coordinateSystemTransition;
    }

    public CoordinateSystemTransition coordinateSystemTransition;

}
using AUIT.AdaptationTriggers;

public class AdaptationTriggerContextSource : ContextSource<AdaptationTrigger>
{
    public override AdaptationTrigger GetValue()
    {
        return adaptationTrigger;
    }
    
    public AdaptationTrigger adaptationTrigger;

}
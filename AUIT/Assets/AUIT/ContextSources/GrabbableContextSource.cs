using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

public struct Unit { }

public class GrabbableContextSource : ContextSource<Grabbable>
{
    public override Grabbable GetValue()
    {
        return grabbable;
    }
    
    public Grabbable grabbable;


}

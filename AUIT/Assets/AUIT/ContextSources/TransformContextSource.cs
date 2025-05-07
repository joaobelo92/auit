using UnityEngine;

public class TransformContextSource : ContextSource<Transform>
{
    public Transform contextSource;

    public override Transform GetValue()
    {
        return contextSource;
    }
}

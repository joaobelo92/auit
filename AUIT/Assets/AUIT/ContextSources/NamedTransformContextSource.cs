using UnityEngine;

public class NamedTransformContextSource : ContextSource<Transform>
{
    public string m_objectName;
    public Transform m_objects;

    public override Transform GetValue()
    {
        return m_objects.Find(m_objectName);
    }
}

using UnityEngine;

public abstract class ContextSource<T> : MonoBehaviour
{
    public abstract T GetValue();
}

using System.Collections;
using UnityEngine;

public class RotateOverAxis : MonoBehaviour
{
    public float rotationDuration = 10f;

    void OnEnable()
    {
        StartCoroutine(RotateSequence());
    }

    private IEnumerator RotateSequence()
    {
        while (true)
        {
            yield return StartCoroutine(RotateOverTime(Vector3.forward, 360f, rotationDuration));
            yield return StartCoroutine(RotateOverTime(Vector3.up, 360f, rotationDuration));
            yield return StartCoroutine(RotateOverTime(Vector3.right, 360f, rotationDuration));
        }
    }
    
    private IEnumerator RotateOverTime(Vector3 axis, float angle, float duration)
    {
        float elapsed = 0f;
        float currentAngle = 0f;

        while (elapsed < duration)
        {
            float step = angle / duration * Time.deltaTime;
            transform.Rotate(axis, step, Space.Self);

            elapsed += Time.deltaTime;
            currentAngle += step;
            yield return null;
        }

        transform.Rotate(axis, angle - currentAngle, Space.Self);
    }
}

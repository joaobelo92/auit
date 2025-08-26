using UnityEngine;

public class RotateOverAxis : MonoBehaviour
{
    public float rotationX = 45f;
    public float rotationY = 0f;
    public float rotationZ = 0f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.right * rotationX * Time.deltaTime);
        transform.Rotate(Vector3.up * rotationY * Time.deltaTime);
        transform.Rotate(Vector3.forward * rotationZ * Time.deltaTime);
    }
}

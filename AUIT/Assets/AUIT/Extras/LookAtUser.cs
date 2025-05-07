using UnityEngine;

public class LookAtUser : MonoBehaviour
{
    public ContextSource<Transform> userContextSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void UpdateRotation()
    {
        if (userContextSource == null)
        {
            return;
        }
        Transform user = userContextSource.GetValue();
        if (user == null)
        {
            return;
        }
        Vector3 userPosition = user.position;
        Vector3 direction = userPosition - transform.position;
        Vector3 flatDirection = new Vector3(direction.x, 0, direction.z);
        if (flatDirection.magnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(flatDirection);
        }

    }

    // Update is called once per frame
    void Update()
    {
        UpdateRotation();
    }
}

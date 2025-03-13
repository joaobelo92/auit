using UnityEditor;
using UnityEngine;

public class OrbitCameraController : MonoBehaviour
{
    public static Camera ControlCamera;

    [Header("Panning Settings")]
    public float panSpeed = 20f; // Speed of panning

    [Header("Zoom Settings")]
    public float zoomSpeed = 10f; // Speed of zooming

    [Header("Rotation Settings")]
    public float rotationSpeed = 100f; // Speed of rotation

    [Header("Orbit Settings")]
    public Transform orbitPivot; // The pivot point to orbit around

    private Vector3 dragOrigin;
    private bool isPanning = false;
    private bool isRotating = false;
    private bool isOrbiting = false;

    void Start()
    {
        ControlCamera = this.GetComponent<Camera>();
    }


    void Update()
    {
        if (!Application.isFocused)
        {
            return;
        }
        HandlePanning();
        HandleZooming();
        HandleRotation();
        HandleOrbiting();
    }

    void HandlePanning()
    {
        if (Input.GetMouseButtonDown(2))
        {
            dragOrigin = Input.mousePosition;
            isPanning = true;
        }

        if (Input.GetMouseButtonUp(2))
        {
            isPanning = false;
        }

        if (isPanning)
        {
            Vector3 difference = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
            Vector3 move = (-difference.x * panSpeed * transform.right) + (-difference.y * panSpeed * transform.up);

            transform.position += move;

            dragOrigin = Input.mousePosition;
        }
    }

    void HandleZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        Vector3 direction = transform.forward * scroll * zoomSpeed;

        transform.position += direction;
    }

    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftAlt))
        {
            dragOrigin = Input.mousePosition;
            isRotating = true;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }

        if (isRotating)
        {
            Vector3 difference = Input.mousePosition - dragOrigin;
            Vector3 current = transform.eulerAngles;
            Quaternion rotation = Quaternion.Euler(current.x - difference.y * rotationSpeed * Time.deltaTime, current.y + difference.x * rotationSpeed * Time.deltaTime, 0);
            transform.rotation = rotation;

            dragOrigin = Input.mousePosition;
        }
    }

    void HandleOrbiting()
    {
        if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.LeftAlt))
        {
            dragOrigin = Input.mousePosition;
            isOrbiting = true;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isOrbiting = false;
        }

        if (isOrbiting && orbitPivot != null)
        {
            Vector3 difference = Input.mousePosition - dragOrigin;
            float rotationX = -difference.y * rotationSpeed * Time.deltaTime;
            float rotationY = difference.x * rotationSpeed * Time.deltaTime;

            transform.RotateAround(orbitPivot.position, Vector3.up, rotationY);
            transform.RotateAround(orbitPivot.position, transform.right, rotationX);

            dragOrigin = Input.mousePosition;
        }
    }

    public void ResetPosition()
    {
        Vector3 position = orbitPivot.position;
        Vector3 forward = orbitPivot.forward;
        forward.y = 0;
        forward.Normalize();
        transform.position = position - forward + Vector3.up;
        transform.LookAt(orbitPivot);
    }
}

[CustomEditor(typeof(OrbitCameraController))]
public class OrbitCameraControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        OrbitCameraController orbitCameraController = (OrbitCameraController)target;
        if (GUILayout.Button("Reset Position"))
        {
            orbitCameraController.ResetPosition();
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{   
    public float moveSpeed = 5.0f;
    public float mouseSensitivity = 2.0f;
    public float zoomSpeed = 2.0f;
    public float minZoomDistance = 2.0f;
    public float maxZoomDistance = 10.0f;

    private bool isRotating = false;

    private void Update()
    {
        // Keyboard movement
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        float upDownInput = Input.GetAxis("UpDown");

        // Calculate the desired movement direction based on camera rotation
        transform.Translate(
            new Vector3(
                horizontalInput,
                upDownInput,
                verticalInput
            )
            * moveSpeed
            * Time.deltaTime
        );

        // Check for mouse rotation button
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }

        // Mouse rotation
        if (isRotating)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            Vector3 rotation = new Vector3(-mouseY, mouseX, 0.0f);
            transform.eulerAngles += rotation;
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0.0f); // Prevent roll rotation
        }

        // Zooming
        float scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
        Vector3 zoom = transform.forward * scrollWheelInput * zoomSpeed;
        if (Vector3.Distance(transform.position, transform.position + zoom) >= minZoomDistance &&
            Vector3.Distance(transform.position, transform.position + zoom) <= maxZoomDistance)
        {
            transform.Translate(zoom, Space.World);
        }
    }
}

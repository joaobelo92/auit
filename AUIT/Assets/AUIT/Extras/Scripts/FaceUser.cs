using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceUser : MonoBehaviour
{
    private Camera userCam;

    void Start()
    {
        userCam = Camera.main;
    }

    void LateUpdate()
    {
        transform.LookAt(userCam.transform, Vector3.up);
        transform.localRotation = transform.localRotation * Quaternion.Euler(0.0f, 180.0f, 0.0f);
    }
}

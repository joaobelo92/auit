using System.Collections;
using System.Collections.Generic;
using AUIT.AdaptationObjectives;
using AUIT.AdaptationObjectives.Definitions;
using Oculus.Interaction.Samples;
using UnityEngine;

public class AdaptationLogic : MonoBehaviour
{

    private bool _initialized;
    private GameObject _leftArmLower;
    private GameObject _leftArmWrist;
    
    public GameObject musicOptimizer;
    public GameObject menu;
    public Vector3 wristOffset = new Vector3(0, 0, 0);
    public Vector3 rotationOffset = new Vector3(0, 0, 0);
    public GameObject test;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!_initialized && GameObject.Find("Joint LeftArmLower") && GameObject.Find("Joint LeftHandWrist"))
        {
            _initialized = true;
            _leftArmLower = GameObject.Find("Joint LeftArmLower");
            _leftArmWrist = GameObject.Find("Joint LeftHandWrist");
        }
        else if (_initialized)
        {
            var localToWorldMatrix = _leftArmLower.transform.localToWorldMatrix;
            musicOptimizer.transform.position = _leftArmLower.transform.position + 
                                                (Vector3)(localToWorldMatrix * wristOffset);
            // musicOptimizer.transform.rotation = _leftArmWrist.transform.rotation;
            // musicOptimizer.transform.forward = -_leftArmWrist.transform.forward;
            musicOptimizer.transform.up = (_leftArmWrist.transform.position - musicOptimizer.transform.position).normalized;
            musicOptimizer.transform.right = (Vector3)(localToWorldMatrix * Vector3.right);
            musicOptimizer.transform.Rotate(Vector3.right, rotationOffset.x);
            musicOptimizer.transform.Rotate(Vector3.up, rotationOffset.y);
            musicOptimizer.transform.Rotate(Vector3.forward, rotationOffset.z);
        }
    }

    public void OpenApp(GameObject app)
    {
        menu.gameObject.SetActive(false);
        var parent = menu.gameObject.transform.parent;
        app.gameObject.transform.position = parent.position;
        app.gameObject.transform.rotation = parent.rotation;
        app.gameObject.SetActive(true);
    }
}

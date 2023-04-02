using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Samples;
using UnityEngine;

public class AdaptationLogic : MonoBehaviour
{

    private bool _initialized;
    private GameObject _leftArmLower;
    private GameObject _leftArmWrist;
    
    public GameObject musicOptimizer;
    public Vector3 wristOffset = new Vector3(0, 0, 0);
    public Quaternion rotationOffset = new Quaternion();
    
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
            var position = _leftArmLower.transform.position;
            musicOptimizer.transform.position = position + 
                                                (Vector3)(_leftArmLower.transform.localToWorldMatrix * wristOffset);
            musicOptimizer.transform.forward = -_leftArmLower.transform.forward;
            musicOptimizer.transform.up = -(position - _leftArmWrist.transform.position).normalized;
        }
    }
}

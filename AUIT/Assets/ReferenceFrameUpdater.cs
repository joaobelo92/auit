using Meta.XR.Movement;
using Meta.XR.Movement.BodyTrackingForFitness;
using Unity.Collections;
using UnityEngine;

public class ReferenceFrameUpdater : MonoBehaviour
{
    public GameObject upperSpine;


    [SerializeField]
    protected BodyPoseBoneTransforms _bodyPoseTransforms;
    private int[] _parentIndices;
    private NativeArray<MSDKUtility.NativeTransform> _nativePose;


    private void InitializeParentIndices()
    {
#if ISDK_DEFINED
        _parentIndices = new int[_bodyPoseTransforms.BoneTransforms.Count];
        for (int i = 0; i < _parentIndices.Length; i++)
        {
            _parentIndices[i] = FitnessCommon.GetParentIndex(_data, i);
        }
#endif
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeParentIndices();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCollections();
        if (upperSpine == null)
        {
            upperSpine = GameObject.Find(".004SpineUpper");
        }
    }
    
    private void UpdateCollections()
    {
#if ISDK_DEFINED
        var boneTransforms = _bodyPoseTransforms.BoneTransforms;
        if (!_nativePose.IsCreated || _nativePose.Length != boneTransforms.Count)
        {
            _nativePose = new NativeArray<MSDKUtility.NativeTransform>(
                boneTransforms.Count, Allocator.Persistent);
        }

        if (_parentIndices.Length != boneTransforms.Count)
        {
            InitializeParentIndices();
        }

        for (int i = 0; i < boneTransforms.Count; i++)
        {
            _nativePose[i] = new MSDKUtility.NativeTransform(boneTransforms[i]);
        }
#endif
    }
}

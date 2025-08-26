
using Meta.XR.Movement.BodyTrackingForFitness;
using UnityEngine;
using Unity.Collections;
using Meta.XR.Movement;

public class MovingMove : MonoBehaviour
{
    private GameObject upperSpine;
    private bool initialized = false;
    public Vector3 torsoOffsetPosition = new Vector3(0.5f, 1.5f, 0.5f);
    public Vector3 torsoOffsetRotation = new Vector3(0, 0, 0);
    public Vector3 torsoOffsetScale = new Vector3(1, 1, 1);

    
    [SerializeField]
    protected BodyPoseBoneTransforms _bodyPoseTransforms;
    private int[] _parentIndices;
    private NativeArray<MSDKUtility.NativeTransform> _nativePose;
    private void Start()
    {
        InitializeParentIndices();
    }

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

    // Update is called once per frame
    void Update()
    {
        UpdateCollections();
        if (upperSpine == null && !initialized)
        {
            upperSpine = GameObject.Find(".004SpineUpper");
            if (upperSpine != null)
            {
                initialized = true;
                transform.parent = upperSpine.transform;
                transform.localPosition = torsoOffsetPosition;
                transform.localRotation = Quaternion.Euler(torsoOffsetRotation);
                transform.localScale = torsoOffsetScale;
            }
        }
    }

    private void OnDestroy()
    {
        if (_nativePose.IsCreated)
        {
            _nativePose.Dispose();
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

using UnityEngine;
using System;

public class ParaHomeAvatarPoseInfo
{
    public float[] bodyJoints;
    public float[] lHandJoints;
    public float[] rHandJoints;
    public float[] headT;
    public float[] hipT;
    public float[] headTip;
}

public class ParaHomeAvatarPose
{
    public Vector3[] bodyJoints;
    public Vector3[] lHandJoints;
    public Vector3[] rHandJoints;
    public Quaternion headRot;
    public Quaternion hipRot;
    public Vector3 headTip; 
    public ParaHomeAvatarPose(ParaHomeAvatarPoseInfo poseInfo)
    {
        bodyJoints = new Vector3[ParaHomeAvatar.NUM_BODY_JOINTS];
        lHandJoints = new Vector3[ParaHomeAvatar.NUM_HAND_JOINTS];
        rHandJoints = new Vector3[ParaHomeAvatar.NUM_HAND_JOINTS];

        for (int i = 0; i < ParaHomeAvatar.NUM_BODY_JOINTS; i++)
        {
            bodyJoints[i] = new Vector3(poseInfo.bodyJoints[i * 3 + 1], poseInfo.bodyJoints[i * 3 + 2], poseInfo.bodyJoints[i * 3]);
        }
        for (int i = 0; i < ParaHomeAvatar.NUM_HAND_JOINTS; i++)
        {
            lHandJoints[i] = new Vector3(poseInfo.lHandJoints[i * 3 + 1], poseInfo.lHandJoints[i * 3 + 2], poseInfo.lHandJoints[i * 3]);
            rHandJoints[i] = new Vector3(poseInfo.rHandJoints[i * 3 + 1], poseInfo.rHandJoints[i * 3 + 2], poseInfo.rHandJoints[i * 3]);
        }
        Matrix4x4 headT = ParaHomeLoader.ParseMat4x4(poseInfo.headT);
        headRot = Quaternion.LookRotation(headT.GetColumn(2), headT.GetColumn(1)) * Quaternion.Euler(0, 90, 90);

        Matrix4x4 hipT = ParaHomeLoader.ParseMat4x4(poseInfo.hipT);
        hipRot = Quaternion.LookRotation(hipT.GetColumn(2), hipT.GetColumn(1)) * Quaternion.Euler(0, 90, 90);

        headTip = new Vector3(poseInfo.headTip[1], poseInfo.headTip[2], poseInfo.headTip[0]);
    }
    
}
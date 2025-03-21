using System;
using System.Collections.Generic;
using UnityEngine;

public class ParaHomeAvatar : MonoBehaviour
{
    #region Static Fields

    public static int NUM_BODY_JOINTS = 23;
    public static int NUM_HAND_JOINTS = 25;
    public enum BodyJointOrder {
        pHipOrigin,
        jL5S1,
        jL4L3,
        jL1T12,
        jT9T8,
        jT1C7,
        jC1Head,
        jRightT4Shoulder,
        jRightShoulder,
        jRightElbow,
        jRightWrist,
        jLeftT4Shoulder,
        jLeftShoulder,
        jLeftElbow,
        jLeftWrist,
        jRightHip,
        jRightKnee,
        jRightAnkle,
        jRightBallFoot,
        jLeftHip,
        jLeftKnee,
        jLeftAnkle,
        jLeftBallFoot
    };

    public enum LJTOrder {
        jLeftWrist, 
        jLeftFirstCMC, 
        jLeftSecondCMC,
        jLeftThirdCMC,
        jLeftFourthCMC,
        jLeftFifthCMC,
        jLeftFifthMCP,
        jLeftFifthPIP,
        jLeftFifthDIP,
        pLeftFifthTip,
        jLeftFourthMCP,
        jLeftFourthPIP,
        jLeftFourthDIP,
        pLeftFourthTip,
        jLeftThirdMCP,
        jLeftThirdPIP,
        jLeftThirdDIP,
        pLeftThirdTip,
        jLeftSecondMCP,
        jLeftSecondPIP,
        jLeftSecondDIP,
        pLeftSecondTip,
        jLeftFirstMCP,
        jLeftIP,
        pLeftFirstTip
    }

    public enum RJTOrder
    {
        jRightWrist,
        jRightFirstCMC,
        jRightSecondCMC,
        jRightThirdCMC,
        jRightFourthCMC,
        jRightFifthCMC,
        jRightFifthMCP,
        jRightFifthPIP,
        jRightFifthDIP,
        pRightFifthTip,
        jRightFourthMCP,
        jRightFourthPIP,
        jRightFourthDIP,
        pRightFourthTip,
        jRightThirdMCP,
        jRightThirdPIP,
        jRightThirdDIP,
        pRightThirdTip,
        jRightSecondMCP,
        jRightSecondPIP,
        jRightSecondDIP,
        pRightSecondTip,
        jRightFirstMCP,
        jRightIP,
        pRightFirstTip
    }


    #endregion

    #region Public Fields

    public GameObject[] m_bodyJoints;
    public GameObject[] m_lHandJoints;
    public GameObject[] m_rHandJoints;
    public GameObject m_headTip;
    public GameObject m_camera;
    public float m_headOffset;

    #endregion

    #region Public Methods

    public void RenderPose()
    {
        for (int i = 0; i < NUM_BODY_JOINTS; i++)
        {
            if (i == 0) continue;
            Transform joint = m_bodyJoints[i].transform;
            LineRenderer lr = joint.GetComponent<LineRenderer>();
            lr.SetPosition(0, joint.position);
            lr.SetPosition(1, joint.parent.position);
        }
        for (int i = 0; i < NUM_HAND_JOINTS; i++)
        {
            if (i == 0) continue;
            Transform joint = m_lHandJoints[i].transform;
            LineRenderer lr = joint.GetComponent<LineRenderer>();
            lr.SetPosition(0, joint.position);
            lr.SetPosition(1, joint.parent.position);
        }
        for (int i = 0; i < NUM_HAND_JOINTS; i++)
        {
            if (i == 0) continue;
            Transform joint = m_rHandJoints[i].transform;
            LineRenderer lr = joint.GetComponent<LineRenderer>();
            lr.SetPosition(0, joint.position);
            lr.SetPosition(1, joint.parent.position);
        }

        LineRenderer headTipLR = m_headTip.GetComponent<LineRenderer>();
        headTipLR.SetPosition(0, m_headTip.transform.position);
        headTipLR.SetPosition(1, m_bodyJoints[(int)BodyJointOrder.jC1Head].transform.position);
    }

    public void SetPose(ParaHomeAvatarPose pose, Vector3 offsetPos, Quaternion offsetRot)
    {
        Vector3 position; 

        // Root joint 
        position = pose.bodyJoints[0];
        position += offsetPos;
        position = offsetRot * position;
        transform.position = position;

        // Body joints
        for (int i = 1; i < NUM_BODY_JOINTS; i++)
        {
            position = pose.bodyJoints[i];
            position += offsetPos;
            position = offsetRot * position;
            m_bodyJoints[i].transform.position = position;
        }

        // Hand joints
        for (int i = 0; i < NUM_HAND_JOINTS; i++)
        {
            position = pose.lHandJoints[i];
            position += offsetPos;
            position = offsetRot * position;
            m_lHandJoints[i].transform.position = position;

            position = pose.rHandJoints[i];
            position += offsetPos;
            position = offsetRot * position;
            m_rHandJoints[i].transform.position = position;
        }

        position = pose.headTip;
        position += offsetPos;
        position = offsetRot * position;
        m_headTip.transform.position = position;

        Vector3 headUp = (m_headTip.transform.position - m_bodyJoints[(int)BodyJointOrder.jT1C7].transform.position).normalized;

        position = m_bodyJoints[(int)BodyJointOrder.jC1Head].transform.position;
        // position += offsetPos;
        // position = offsetRot * position;
        m_camera.transform.position = position;

        Quaternion rotation = pose.headRot;
        rotation = offsetRot * rotation;
        m_camera.transform.rotation = rotation;

        RenderPose();
    }
    public void Reset()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        for (int i = 1; i < NUM_BODY_JOINTS; i++)
        {
            m_bodyJoints[i].transform.position = Vector3.zero;
        }
        for (int i = 0; i < NUM_HAND_JOINTS; i++)
        {
            m_lHandJoints[i].transform.position = Vector3.zero;
            m_rHandJoints[i].transform.position = Vector3.zero;
        }
        m_headTip.transform.position = Vector3.zero;
        m_camera.transform.position = Vector3.zero;
        m_camera.transform.rotation = Quaternion.identity;
        RenderPose();
    }


    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}

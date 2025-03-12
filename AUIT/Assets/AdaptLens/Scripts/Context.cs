using NUnit.Framework;
using UnityEngine;

public class ParaHomeContext 
{
    public ParaHomeAvatarPose pose;
    public ParaHomeScene scene;

    public ParaHomeContext(ParaHomeAvatarPose pose, ParaHomeScene scene)
    {
        this.pose = pose;
        this.scene = scene;
    }
}

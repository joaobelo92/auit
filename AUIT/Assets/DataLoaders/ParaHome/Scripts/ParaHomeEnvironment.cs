using System;
using System.Collections.Generic;
using UnityEngine;

public class ParaHomeObjectPart
{
    private Vector3 position;
    public Vector3 Position
    {
        get { return position; }
    }

    private Quaternion rotation;
    public Quaternion Rotation
    {
        get { return rotation; }
    }

    public ParaHomeObjectPart(float[] values)
    {
        Matrix4x4 T = ParaHomeLoader.ParseMat4x4(values);
        this.position = T.GetColumn(3);
        this.rotation = Quaternion.LookRotation(T.GetColumn(2), T.GetColumn(1));
    }
}

public class ParaHomeObject
{
    private string name; 
    public string Name
    {
        get { return name; }
    }

    private ParaHomeObjectPart[] parts;
    public ParaHomeObjectPart Base
    {
        get { return parts[0]; }
    }
    public ParaHomeObjectPart Part1
    {
        get { return parts[1]; }
    }
    public ParaHomeObjectPart Part2
    {
        get { return parts[2]; }
    }
    public ParaHomeObject(string name, Dictionary<string, float[]> objectInfo)
    {
        this.name = name;
        int numParts = objectInfo.Count;
        this.parts = new ParaHomeObjectPart[ParaHomeLoader.OBJ_PARTS.Length];
        foreach (string part in ParaHomeLoader.OBJ_PARTS)
        {
            if (objectInfo.ContainsKey(part))
            {
                this.parts[Array.IndexOf(ParaHomeLoader.OBJ_PARTS, part)] = new ParaHomeObjectPart(objectInfo[part]);
            }
        }
    }
}

public class ParaHomeScene
{
    private ParaHomeObject[] objects;
    public ParaHomeObject[] Objects
    {
        get { return objects; }
    }

    public ParaHomeScene(Dictionary<string, Dictionary<string, float[]>> environmentInfo)
    {
        int numObjects = environmentInfo.Count;
        this.objects = new ParaHomeObject[numObjects];
        int oi = 0;
        foreach (string obj in environmentInfo.Keys)
        {
            this.objects[oi++] = new ParaHomeObject(obj, environmentInfo[obj]);
        }
    }

}

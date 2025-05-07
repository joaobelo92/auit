using System;
using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false)]
public class Parameter : PropertyAttribute
{
    public string name { get; private set; }

    public Parameter(string name = null)
    {
        this.name = name;
    }
}
